
using PayChangu.NET.Extensions;
using PayChangu.NET.Models;
using Xunit;

namespace PayChangu.NET.Tests;

public sealed class VerifyPaymentResponseExtensionsTests
{
    private const string ExpectedTransactionIdentifier = "TXN-123456";
    private const string ExpectedCurrency = "MWK";
    private const decimal ExpectedAmount = 5000m;

    private static VerifyPaymentResponse CreateResponse()
    {
        return new VerifyPaymentResponse
        {
            Status = "success",
            ApiStatus = "success",
            TransactionIdentifier = ExpectedTransactionIdentifier,
            Currency = ExpectedCurrency,
            Amount = ExpectedAmount
        };
    }

    [Fact]
    public void IsValid_WhenAllExpectedValuesMatch_ReturnsTrue()
    {
        // Arrange
        var response = CreateResponse();

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("SUCCESS")]
    [InlineData("Success")]
    [InlineData("success")]
    public void IsValid_WhenStatusIsSuccess_IgnoresCase(string status)
    {
        // Arrange
        var response = CreateResponse();
        response.Status = status;

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("failed")]
    [InlineData("cancelled")]
    [InlineData("successful")]
    [InlineData("")]
    public void IsValid_WhenTransactionStatusIsNotSuccess_ReturnsFalse(
        string status)
    {
        // Arrange
        var response = CreateResponse();
        response.Status = status;

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WhenTransactionIdentifierDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var response = CreateResponse();

        // Act
        var result = response.IsValid(
            "TXN-999999",
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WhenCurrencyDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var response = CreateResponse();

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            "USD",
            ExpectedAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WhenAmountDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var response = CreateResponse();

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            6000m);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WhenResponseIsNull_ReturnsFalse()
    {
        // Arrange
        VerifyPaymentResponse? response = null;

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_WhenApiStatusIsSuccessButTransactionStatusIsFailed_ReturnsFalse()
    {
        // Arrange
        var response = CreateResponse();
        response.ApiStatus = "success";
        response.Status = "failed";

        // Act
        var result = response.IsValid(
            ExpectedTransactionIdentifier,
            ExpectedCurrency,
            ExpectedAmount);

        // Assert
        Assert.False(result);
    }
}