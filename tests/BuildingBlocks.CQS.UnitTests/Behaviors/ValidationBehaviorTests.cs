using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using TicketFlow.BuildingBlocks.CQS.Abstractions.Exceptions.Validation;
using TicketFlow.BuildingBlocks.CQS.Behaviors;

namespace TicketFlow.BuildingBlocks.CQS.UnitTests.Behaviors;

/// <summary>
/// Verifies request validation, error aggregation, and pipeline execution.
/// </summary>
public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidatorsExist_ExecutesHandlerAndForwardsCancellationToken()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();

        var request = new TestRequest("Alice", "alice@example.com");
        var expectedResponse = new TestResponse(42);
        var handler = new HandlerSpy(expectedResponse);
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([]);

        // Act
        var response = await behavior.Handle(
            request,
            handler.Handle,
            cancellationSource.Token);

        // Assert
        response.Should().BeSameAs(expectedResponse);
        handler.CallCount.Should().Be(1);
        handler.ObservedToken.Should().Be(cancellationSource.Token);
    }

    [Fact]
    public async Task Handle_WhenAllValidatorsSucceed_ValidatesBeforeExecutingHandler()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();

        var request = new TestRequest("Alice", "alice@example.com");
        var expectedResponse = new TestResponse(42);
        var executionOrder = new List<string>();

        var firstValidator = CreateValidator();
        var secondValidator = CreateValidator();

        // Yield inside validation to verify that asynchronous work is awaited.
        firstValidator
            .Setup(validator => validator.ValidateAsync(request, cancellationSource.Token))
            .Returns(async () =>
            {
                executionOrder.Add("First validator started");

                await Task.Yield();

                executionOrder.Add("First validator finished");

                return new ValidationResult();
            });

        secondValidator
            .Setup(validator => validator.ValidateAsync(request, cancellationSource.Token))
            .Returns(() =>
            {
                executionOrder.Add("Second validator finished");

                return Task.FromResult(new ValidationResult());
            });

        var handler = new HandlerSpy(expectedResponse)
        {
            OnHandle = () => executionOrder.Add("Handler executed")
        };

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(
            new List<IValidator<TestRequest>>
            {
                firstValidator.Object,
                secondValidator.Object
            });

        // Act
        var response = await behavior.Handle(request, handler.Handle, cancellationSource.Token);

        // Assert
        response.Should().BeSameAs(expectedResponse);
        handler.CallCount.Should().Be(1);
        handler.ObservedToken.Should().Be(cancellationSource.Token);

        executionOrder.Should().Equal(
            "First validator started",
            "First validator finished",
            "Second validator finished",
            "Handler executed");

        firstValidator.Verify(
            validator => validator.ValidateAsync(request, cancellationSource.Token),
            Times.Once);

        secondValidator.Verify(
            validator => validator.ValidateAsync(request, cancellationSource.Token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOneValidatorFails_ThrowsValidationExceptionWithoutExecutingHandler()
    {
        // Arrange
        var validator = CreateValidator(
            new ValidationFailure(nameof(TestRequest.Name), "Name is required."));

        var handler = new HandlerSpy();
        var behavior = CreateBehavior(validator);

        // Act
        var action = () => behavior.Handle(
            new TestRequest("", "alice@example.com"),
            handler.Handle,
            CancellationToken.None);

        // Assert
        var assertion = await action.Should().ThrowAsync<RequestValidationException>();

        assertion.Which.Errors.Should().ContainSingle();
        assertion.Which.Errors[nameof(TestRequest.Name)].Should().Equal("Name is required.");
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSeveralValidatorsFail_CollectsAllErrorsByField()
    {
        // Arrange
        var firstValidator = CreateValidator(
            new ValidationFailure(nameof(TestRequest.Name), "Name is required."));

        var successfulValidator = CreateValidator();

        var lastValidator = CreateValidator(
            new ValidationFailure(nameof(TestRequest.Name), "Name must contain at least two characters."),
            new ValidationFailure(nameof(TestRequest.Email), "Email is invalid."));

        var request = new TestRequest("", "invalid-email");
        var handler = new HandlerSpy();
        var behavior = CreateBehavior(firstValidator, successfulValidator, lastValidator);

        // Act
        var action = () => behavior.Handle(request, handler.Handle, CancellationToken.None);

        // Assert
        var assertion = await action.Should().ThrowAsync<RequestValidationException>();
        var errors = assertion.Which.Errors;

        errors.Should().HaveCount(2);

        errors[nameof(TestRequest.Name)].Should().Equal(
            "Name is required.",
            "Name must contain at least two characters.");

        errors[nameof(TestRequest.Email)].Should().Equal("Email is invalid.");
        handler.CallCount.Should().Be(0);

        // An invalid result must not prevent the remaining validators from running.
        foreach (var validator in new[] { firstValidator, successfulValidator, lastValidator })
        {
            validator.Verify(
                instance => instance.ValidateAsync(request, CancellationToken.None),
                Times.Once);
        }
    }

    [Fact]
    public async Task Handle_WhenMessagesRepeat_RemovesDuplicatesWithinEachFieldOnly()
    {
        // Arrange
        const string requiredMessage = "A value is required.";

        var firstValidator = CreateValidator(
            new ValidationFailure(nameof(TestRequest.Name), requiredMessage),
            new ValidationFailure(nameof(TestRequest.Name), requiredMessage));

        var secondValidator = CreateValidator(
            new ValidationFailure(nameof(TestRequest.Name), requiredMessage),
            new ValidationFailure(nameof(TestRequest.Name), "Name is too short."),
            new ValidationFailure(nameof(TestRequest.Email), requiredMessage));

        var handler = new HandlerSpy();
        var behavior = CreateBehavior(firstValidator, secondValidator);

        // Act
        var action = () => behavior.Handle(
            new TestRequest("", ""),
            handler.Handle,
            CancellationToken.None);

        // Assert
        var assertion = await action.Should().ThrowAsync<RequestValidationException>();
        var errors = assertion.Which.Errors;

        errors.Should().HaveCount(2);
        errors[nameof(TestRequest.Name)].Should().Equal(requiredMessage, "Name is too short.");
        errors[nameof(TestRequest.Email)].Should().Equal(requiredMessage);
        handler.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenErrorAppliesToEntireRequest_PreservesEmptyFieldName()
    {
        // Arrange
        // Object-level validation may report an error without a property name.
        var validator = CreateValidator(
            new ValidationFailure(string.Empty, "The request contains incompatible values."));

        var handler = new HandlerSpy();
        var behavior = CreateBehavior(validator);

        // Act
        var action = () => behavior.Handle(
            new TestRequest("Alice", "alice@example.com"),
            handler.Handle,
            CancellationToken.None);

        // Assert
        var assertion = await action.Should().ThrowAsync<RequestValidationException>();

        assertion.Which.Errors.Should().ContainSingle();
        assertion.Which.Errors[string.Empty].Should().Equal(
            "The request contains incompatible values.");

        handler.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_WithRealAsyncValidator_UsesAsyncValidation(bool isValid)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();

        var observedToken = CancellationToken.None;
        var validator = new InlineValidator<TestRequest>();

        validator.RuleFor(request => request.Name)
            .MustAsync(async (_, cancellationToken) =>
            {
                observedToken = cancellationToken;

                await Task.Yield();

                return isValid;
            })
            .WithMessage("Name is unavailable.");

        var handler = new HandlerSpy();
        var behavior = new ValidationBehavior<TestRequest, TestResponse>([validator]);

        // Act
        var action = () => behavior.Handle(
            new TestRequest("Alice", "alice@example.com"),
            handler.Handle,
            cancellationSource.Token);

        // Assert
        if (isValid)
        {
            var response = await action();

            response.Should().BeSameAs(handler.Response);
            handler.CallCount.Should().Be(1);
        }
        else
        {
            var assertion = await action.Should().ThrowAsync<RequestValidationException>();

            assertion.Which.Errors[nameof(TestRequest.Name)].Should().Equal("Name is unavailable.");
            handler.CallCount.Should().Be(0);
        }

        observedToken.Should().Be(cancellationSource.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_WhenValidatorThrows_PropagatesOriginalExceptionAndStopsPipeline(
        bool isCancellation)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();

        if (isCancellation)
        {
            await cancellationSource.CancelAsync();
        }

        Exception expectedException = isCancellation
            ? new OperationCanceledException("Validation was canceled.", cancellationSource.Token)
            : new InvalidOperationException("Validation dependency failed.");

        var failingValidator = CreateValidator();

        failingValidator
            .Setup(validator => validator.ValidateAsync(
                It.IsAny<TestRequest>(),
                cancellationSource.Token))
            .ThrowsAsync(expectedException);

        var remainingValidator = CreateValidator();
        var handler = new HandlerSpy();
        var behavior = CreateBehavior(failingValidator, remainingValidator);

        // Act
        var action = () => behavior.Handle(
            new TestRequest("Alice", "alice@example.com"),
            handler.Handle,
            cancellationSource.Token);

        // Assert
        var assertion = await action.Should().ThrowAsync<Exception>();

        // Infrastructure failures and cancellation must not become validation errors.
        assertion.Which.Should().BeSameAs(expectedException);
        handler.CallCount.Should().Be(0);

        remainingValidator.Verify(
            validator => validator.ValidateAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Handle_WhenHandlerThrows_PropagatesOriginalException(
        bool hasValidator,
        bool isCancellation)
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();

        Exception expectedException = isCancellation
            ? new OperationCanceledException("Handler was canceled.", cancellationSource.Token)
            : new InvalidOperationException("Handler failed.");

        var behavior = hasValidator
            ? CreateBehavior(CreateValidator())
            : new ValidationBehavior<TestRequest, TestResponse>([]);

        var handler = new HandlerSpy
        {
            ExceptionToThrow = expectedException
        };

        // Act
        var action = () => behavior.Handle(
            new TestRequest("Alice", "alice@example.com"),
            handler.Handle,
            cancellationSource.Token);

        // Assert
        var assertion = await action.Should().ThrowAsync<Exception>();

        assertion.Which.Should().BeSameAs(expectedException);
        handler.CallCount.Should().Be(1);
        handler.ObservedToken.Should().Be(cancellationSource.Token);
    }

    [Fact]
    public async Task Handle_WhenHandlerReturnsNull_PreservesNullResponse()
    {
        // Arrange
        var behavior = CreateBehavior(CreateValidator());

        // Act
        var response = await behavior.Handle(
            new TestRequest("Alice", "alice@example.com"),
            _ => Task.FromResult<TestResponse>(null!),
            CancellationToken.None);

        // Assert
        response.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenBehaviorIsReused_DoesNotRetainErrorsFromPreviousRequest()
    {
        // Arrange
        var invalidRequest = new TestRequest("", "alice@example.com");
        var validRequest = new TestRequest("Alice", "alice@example.com");
        var validator = CreateValidator();

        validator
            .Setup(instance => instance.ValidateAsync(invalidRequest, CancellationToken.None))
            .ReturnsAsync(new ValidationResult(
                [new ValidationFailure(nameof(TestRequest.Name), "Name is required.")]));

        var handler = new HandlerSpy();
        var behavior = CreateBehavior(validator);

        var invalidAction = () => behavior.Handle(
            invalidRequest,
            handler.Handle,
            CancellationToken.None);

        await invalidAction.Should().ThrowAsync<RequestValidationException>();

        handler.CallCount.Should().Be(0);

        // Act
        var response = await behavior.Handle(validRequest, handler.Handle, CancellationToken.None);

        // Assert
        response.Should().BeSameAs(handler.Response);
        handler.CallCount.Should().Be(1);
    }

    /// <summary>
    /// Creates a validator with a predictable result for any request and token.
    /// Individual tests can override this setup for a specific scenario.
    /// </summary>
    private static Mock<IValidator<TestRequest>> CreateValidator(params ValidationFailure[] failures)
    {
        var validator = new Mock<IValidator<TestRequest>>(MockBehavior.Strict);

        validator
            .Setup(instance => instance.ValidateAsync(
                It.IsAny<TestRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        return validator;
    }

    /// <summary>
    /// Supplies validators as a lazy sequence to exercise enumerable registration support.
    /// </summary>
    private static ValidationBehavior<TestRequest, TestResponse> CreateBehavior(
        params Mock<IValidator<TestRequest>>[] validators)
    {
        return new ValidationBehavior<TestRequest, TestResponse>(
            validators.Select(validator => validator.Object));
    }

    // Moq's generated validator proxy requires a publicly accessible request type.
    public sealed record TestRequest(string Name, string Email) : IRequest<TestResponse>;

    public sealed record TestResponse(int Value);

    /// <summary>
    /// Records handler execution without obscuring the arrange, act, and assert steps.
    /// </summary>
    private sealed class HandlerSpy(TestResponse? response = null)
    {
        public TestResponse Response { get; } = response ?? new TestResponse(42);

        public int CallCount { get; private set; }

        public CancellationToken ObservedToken { get; private set; }

        public Action? OnHandle { get; init; }

        public Exception? ExceptionToThrow { get; init; }

        public Task<TestResponse> Handle(CancellationToken cancellationToken)
        {
            CallCount++;
            ObservedToken = cancellationToken;

            OnHandle?.Invoke();

            return ExceptionToThrow is not null
                ? Task.FromException<TestResponse>(ExceptionToThrow)
                : Task.FromResult(Response);
        }
    }
}
