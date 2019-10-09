// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace PartsInventoryConnector.Authentication
{
    public class ClientCredentialAuthProvider : IAuthenticationProvider
    {
        private IConfidentialClientApplication _msalClient;
        private int _maxRetries = 3;

        public ClientCredentialAuthProvider(string appId, string tenantId, string secret)
        {
            _msalClient = ConfidentialClientApplicationBuilder
                .Create(appId)
                .WithTenantId(tenantId)
                .WithClientSecret(secret)
                .Build();
        }

        public async Task AuthenticateRequestAsync(HttpRequestMessage request)
        {
            int retryCount = 0;

            do
            {
                try
                {
                    var result = await _msalClient
                        .AcquireTokenForClient(new []{"https://graph.microsoft.com/.default"})
                        .ExecuteAsync();

                    if (!string.IsNullOrEmpty(result.AccessToken))
                    {
                        request.Headers.Authorization =
                            new AuthenticationHeaderValue("bearer", result.AccessToken);
                        break;
                    }
                }
                catch (MsalServiceException serviceException)
                {
                    if (serviceException.ErrorCode == "temporarily_unavailable")
                    {
                        var delay = GetRetryAfter(serviceException);
                        await Task.Delay(delay);
                    }
                    else
                    {
                        throw new AuthenticationException(
                            new Error {
                                Code = "generalException",
                                Message = "Unexpected exception returned from MSAL."
                            },
                            serviceException
                        );
                    }
                }
                catch (Exception exception)
                {
                    throw new AuthenticationException(
                        new Error {
                            Code = "generalException",
                            Message = "Unexpected exception occurred while authenticating the request."
                        },
                        exception
                    );
                }

                retryCount++;
            } while (retryCount < _maxRetries);
        }

        private TimeSpan GetRetryAfter(MsalServiceException serviceException)
        {
            var retryAfter = serviceException.Headers?.RetryAfter;
            TimeSpan? delay = null;

            if (retryAfter != null && retryAfter.Delta.HasValue)
            {
                delay = retryAfter.Delta;
            }
            else if (retryAfter != null && retryAfter.Date.HasValue)
            {
                delay = retryAfter.Date.Value.Offset;
            }

            if (delay == null)
            {
                throw new MsalServiceException(
                    serviceException.ErrorCode,
                    "Missing Retry-After header."
                );
            }

            return delay.Value;
        }
    }
}