using CleanArchitecture.BuildingBlocks.Messaging;

namespace JobService.Application.Jobs.Queries.GetJobById;

public sealed record GetJobByIdQuery(Guid JobId) : IQuery<JobResponse>;
