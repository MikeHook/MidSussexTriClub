using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using GoCardless;
using GoCardless.Exceptions;
using GoCardless.Services;
using Mstc.Core.Dto;
using Newtonsoft.Json;

namespace Mstc.Core.Providers
{
	public class GoCardlessProvider
	{
	    private GoCardlessClient _client;
		public GoCardlessClient.Environment Environment;

        public GoCardlessProvider()
		{
			System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			Environment = ConfigurationManager.AppSettings["gocardlessEnvironment"] == "Production"
		        ? GoCardlessClient.Environment.LIVE
		        : GoCardlessClient.Environment.SANDBOX;

		    _client = GoCardlessClient.Create(ConfigurationManager.AppSettings["gocardlessAccessToken"],
				Environment);
		}

	    public RedirectResponseDto CreateRedirectRequest(Umbraco.Core.Logging.ILogger logger, CustomerDto customer, string description, string sessionToken, string successUrl)
	    {
			var request = new RedirectFlowCreateRequest()
			{
				Description = description,
				SessionToken = sessionToken,
				SuccessRedirectUrl = successUrl,
				// Optionally, prefill customer details on the payment page
				PrefilledCustomer = new RedirectFlowCreateRequest.RedirectFlowPrefilledCustomer()
				{
					GivenName = customer.GivenName,
					FamilyName = customer.FamilyName,
					Email = customer.Email,
					AddressLine1 = customer.AddressLine1,
					City = customer.City,
					PostalCode = customer.PostalCode
				}
			};

			return TryCreateRedirectRequest(logger, customer, request);
		}

		public RedirectResponseDto TryCreateRedirectRequest(Umbraco.Core.Logging.ILogger logger, CustomerDto customer, RedirectFlowCreateRequest request)
		{
			try
			{
				RedirectFlowResponse redirectFlowResponse = _client.RedirectFlows.CreateAsync(request).Result;
				var redirectFlow = redirectFlowResponse.RedirectFlow;
				return new RedirectResponseDto()
				{
					Id = redirectFlow.Id,
					RedirectUrl = redirectFlow.RedirectUrl
				};
			}
			catch (Exception ex)
			{
				string error = "GoCardless Error setting up mandate.";
				logger.Error(typeof(GoCardlessProvider), string.Format(
						$"Unable to CreateRedirectRequest for memberEmail: {customer.Email}, exception: {0}",
						ex), ex);

				var exception = ex.InnerException;
				if (exception != null)
				{
					var apiException = exception as ApiException;

					if (apiException != null)
					{
						var errors = JsonConvert.SerializeObject(apiException?.Errors);
						error = $"GoCardless Error setting up mandate - {errors}";
						logger.Warn(typeof(GoCardlessProvider), errors);		
						logger.Error(typeof(GoCardlessProvider), JsonConvert.SerializeObject(apiException), apiException);
					}
					else
					{
						logger.Error(typeof(GoCardlessProvider), $"GoCardless Error: {exception.ToString()}", exception);
					}
				}

				if (request.PrefilledCustomer != null)
				{
					request.PrefilledCustomer = null;
					return TryCreateRedirectRequest(logger, customer, request);
				}
				else
				{
					return new RedirectResponseDto() { Error = error };
				}
			}
		}

		public string CompleteRedirectRequest(string requestId, string sessionToken)
	    {
	        var redirectFlowResponse = _client.RedirectFlows
	            .CompleteAsync(requestId,
	                new RedirectFlowCompleteRequest()
	                {
	                    SessionToken = sessionToken
	                }
	            ).Result;

	        return redirectFlowResponse.RedirectFlow.Links.Mandate;
	    }

	    public PaymentResponseDto CreatePayment(Umbraco.Core.Logging.ILogger logger, string memberMandateId, string memberEmail, int costInPence, string description)
	    {
            int retries = 5;
            int tried = 0;
	        string idempotencyKey = Guid.NewGuid().ToString();
            return TryCreatePayment(logger, idempotencyKey, memberMandateId, memberEmail, costInPence, description, retries, tried);
        }

	    public PaymentResponseDto TryCreatePayment(Umbraco.Core.Logging.ILogger logger, string idempotencyKey, string memberMandateId, string memberEmail,
	        int costInPence, string description, int retries, int tried)
	    {
	        if (tried >= retries)
	        {
	            return PaymentResponseDto.UnknownError;
	        }

	        try
	        {
	            tried++;

	            var createResponse = _client.Payments.CreateAsync(new PaymentCreateRequest()
	            {
	                Amount = costInPence,
	                Currency = PaymentCreateRequest.PaymentCurrency.GBP,
	                Links = new PaymentCreateRequest.PaymentLinks()
	                {
	                    Mandate = memberMandateId,
	                },
	                Description = description,
	                IdempotencyKey = idempotencyKey
	            }).Result;

	            string message =
	                $"New CreatePayment request. memberEmail: {memberEmail}, memberMandateId: {memberMandateId}, " +
	                $"cost: {costInPence}, description: {description}";
				logger.Info(typeof(GoCardlessProvider), message);			

	            return PaymentResponseDto.Success;
	        }
	        catch (Exception ex)
	        {
				logger.Error(typeof(GoCardlessProvider), string.Format(
						$"Unable to CreatePayment for memberEmail: {memberEmail}, memberMandateId: {memberMandateId},, exception: {0}",
						ex), ex);		

	            var exception = ex.InnerException as ApiException;
	            if (exception != null)
	            {
	                var mandateErrors = new List<string>() {"Mandate is failed, cancelled or expired", "Mandate not found"};
	                if (mandateErrors.Contains(exception.ApiErrorResponse.Error.Message))
	                {
	                    return PaymentResponseDto.MandateError;
	                }
	            }

	            return TryCreatePayment(logger, idempotencyKey, memberMandateId, memberEmail, costInPence, description, retries,
	                tried);
	        }
	    }
	}
}