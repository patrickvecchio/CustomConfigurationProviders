using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Configuration.Providers.Tests.AwsSecretsManager.Mocks
{
    public class MockAwsSecretsManager : IAmazonSecretsManager
    {
        private readonly Dictionary<string, object> _secretsData;

        public MockAwsSecretsManager(Dictionary<string,object> secretsData)
        {
            _secretsData = secretsData;
        }

        public Task<GetSecretValueResponse> GetSecretValueAsync(GetSecretValueRequest request, CancellationToken cancellationToken = default)
        {
            var data = JsonSerializer.Serialize(_secretsData[request.SecretId]);
            return Task.FromResult<GetSecretValueResponse>(new GetSecretValueResponse
            {
                ARN = string.Empty,
                ContentLength = data.Length,
                CreatedDate = DateTime.UtcNow,
                HttpStatusCode = HttpStatusCode.OK,
                Name = string.Empty,
                ResponseMetadata = new ResponseMetadata
                {
                    ChecksumAlgorithm = CoreChecksumAlgorithm.SHA256,
                    ChecksumValidationStatus = ChecksumValidationStatus.SUCCESSFUL,
                    RequestId = string.Empty
                },
                SecretString = data
            });;
        }


        public ISecretsManagerPaginatorFactory Paginators => throw new NotImplementedException();

        public IClientConfig Config => throw new NotImplementedException();

        public Task<CancelRotateSecretResponse> CancelRotateSecretAsync(CancelRotateSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<CreateSecretResponse> CreateSecretAsync(CreateSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteResourcePolicyResponse> DeleteResourcePolicyAsync(DeleteResourcePolicyRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DeleteSecretResponse> DeleteSecretAsync(DeleteSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<DescribeSecretResponse> DescribeSecretAsync(DescribeSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task<GetRandomPasswordResponse> GetRandomPasswordAsync(GetRandomPasswordRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<GetResourcePolicyResponse> GetResourcePolicyAsync(GetResourcePolicyRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListSecretsResponse> ListSecretsAsync(ListSecretsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ListSecretVersionIdsResponse> ListSecretVersionIdsAsync(ListSecretVersionIdsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PutResourcePolicyResponse> PutResourcePolicyAsync(PutResourcePolicyRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PutSecretValueResponse> PutSecretValueAsync(PutSecretValueRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RemoveRegionsFromReplicationResponse> RemoveRegionsFromReplicationAsync(RemoveRegionsFromReplicationRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ReplicateSecretToRegionsResponse> ReplicateSecretToRegionsAsync(ReplicateSecretToRegionsRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RestoreSecretResponse> RestoreSecretAsync(RestoreSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<RotateSecretResponse> RotateSecretAsync(RotateSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<StopReplicationToReplicaResponse> StopReplicationToReplicaAsync(StopReplicationToReplicaRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<TagResourceResponse> TagResourceAsync(TagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UntagResourceResponse> UntagResourceAsync(UntagResourceRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSecretResponse> UpdateSecretAsync(UpdateSecretRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<UpdateSecretVersionStageResponse> UpdateSecretVersionStageAsync(UpdateSecretVersionStageRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ValidateResourcePolicyResponse> ValidateResourcePolicyAsync(ValidateResourcePolicyRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
