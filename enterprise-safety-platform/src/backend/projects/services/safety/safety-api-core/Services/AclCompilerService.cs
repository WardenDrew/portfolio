using Platform.Legacy.Core.Extensions.ServiceScanning;
using Platform.Legacy.Data.Entities.AccessControlLists;
using Platform.Legacy.Data.Entities.Companies;
using Platform.Legacy.Data.Entities.Users;

namespace Platform.Legacy.Core.Services;

public interface IAclCompilerService : IServiceScanningServiceInterface
{
	Task ChangedCompany(int companyId, CancellationToken cancellationToken = default);
	Task ChangedGroup(int groupId, CancellationToken cancellationToken = default);
	Task ChangedJob(int jobId, CancellationToken cancellationToken = default);
	Task ChangedUser(int userId, CancellationToken cancellationToken = default);
	Task RecalculateAclAsync(int aclId, CancellationToken cancellationToken = default);
}

public class AclCompilerService(LegacyDbContext db) : IAclCompilerService, IServiceScanningScopedImplementation
{
	public async Task ChangedCompany(int companyId, CancellationToken cancellationToken = default)
	{
		List<int> aclIds = await db.Set<AccessControlList>()
			.Where(x => x.CompanyId == companyId)
			.Select(x => x.Id)
			.ToListAsync(cancellationToken);

		foreach (int aclId in aclIds)
		{
			await RecalculateAclAsync(aclId: aclId, cancellationToken: cancellationToken);
		}
	}

	public async Task ChangedUser(int userId, CancellationToken cancellationToken = default)
	{
		HashSet<int> aclIds = [];

		aclIds.UnionWith(
			await db.Set<AccessControlList>()
				.Where(x => x.AllCompany == true)
				.Where(x => x.Company.Users.Where(y => y.Id == userId).Any())
				.Select(x => x.Id)
				.ToListAsync(cancellationToken)
		);

		aclIds.UnionWith(
			await db.Set<AccessControlListUser>()
				.Where(x => x.UserId == userId)
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		aclIds.UnionWith(
			await db.Set<AccessControlListGroup>()
				.Where(x => x.Group.GroupUsers.Where(y => y.UserId == userId).Any())
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		aclIds.UnionWith(
			await db.Set<AccessControlListJob>()
				.Where(x => x.Job.JobGroups.Where(y => y.Group.GroupUsers.Where(z => z.UserId == userId).Any()).Any())
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		aclIds.UnionWith(
			await db.Set<AccessControlListJob>()
				.Where(x => x.Job.JobUsers.Where(y => y.UserId == userId).Any())
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		foreach (int aclId in aclIds)
		{
			await RecalculateAclAsync(aclId: aclId, cancellationToken: cancellationToken);
		}
	}

	public async Task ChangedGroup(int groupId, CancellationToken cancellationToken = default)
	{
		HashSet<int> aclIds = [];

		aclIds.UnionWith(
			await db.Set<AccessControlListGroup>()
				.Where(x => x.GroupId == groupId)
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		aclIds.UnionWith(
			await db.Set<AccessControlListJob>()
				.Where(x => x.Job.JobGroups.Where(y => y.GroupId == groupId).Any())
				.Select(x => x.AccessControlListId)
				.ToListAsync(cancellationToken)
		);

		foreach (int aclId in aclIds)
		{
			await RecalculateAclAsync(aclId: aclId, cancellationToken: cancellationToken);
		}
	}

	public async Task ChangedJob(int jobId, CancellationToken cancellationToken = default)
	{
		List<int> aclIds = await db.Set<AccessControlListJob>()
			.Where(x => x.JobId == jobId)
			.Select(x => x.AccessControlListId)
			.ToListAsync(cancellationToken);

		foreach (int aclId in aclIds)
		{
			await RecalculateAclAsync(aclId: aclId, cancellationToken: cancellationToken);
		}
	}

	public async Task RecalculateAclAsync(int aclId, CancellationToken cancellationToken = default)
	{
		AccessControlList? acl = await db.Set<AccessControlList>()
			.Include(x => x.AccessControlListUsers)
			.Include(x => x.AccessControlListGroups)
			.Include(x => x.AccessControlListJobs)
			.Where(x => x.Id == aclId)
			.FirstOrDefaultAsync(cancellationToken);
		if (acl is null)
		{
			throw new NullReferenceException($"Invalid ACL Id passed to {nameof(AclCompilerService.RecalculateAclAsync)}!");
		}

		// Remove effective users (happens immediately without a savechanges)
		_ = await db.Set<AccessControlListEffectiveUser>()
			.Where(x => x.AccessControlListId == acl.Id)
			.ExecuteDeleteAsync(cancellationToken);

		// Start new hashset to track effective users
		HashSet<int> effectiveUsers = [];

		// Check All Company Value if this is set, early exit
		if (acl.AllCompany.HasTrueValue())
		{
			effectiveUsers.UnionWith(
				await db.Set<User>()
					.Where(x => x.CompanyId == acl.CompanyId)
					.Where(x => x.Status != Status.DELETED)
					.Select(x => x.Id)
					.ToListAsync(cancellationToken)
			);

			// Re-compute effective users
			foreach (int effectiveUserId in effectiveUsers)
			{
				_ = db.Add(new AccessControlListEffectiveUser() { AccessControlList = acl, UserId = effectiveUserId, });
			}

			_ = await db.SaveChangesAsync(cancellationToken);

			return;
		}

		// Check individual users
		foreach (AccessControlListUser aclUser in acl.AccessControlListUsers)
		{
			bool valid = await db.Set<User>()
				.Where(x => x.Id == aclUser.Id)
				.Where(x => x.CompanyId == acl.CompanyId)
				.Where(x => x.Status != Status.DELETED)
				.AnyAsync(cancellationToken);

			if (!valid)
			{
				_ = db.Remove(aclUser);
				continue;
			}

			_ = effectiveUsers.Add(aclUser.UserId);
		}

		// Check Groups
		foreach (AccessControlListGroup aclGroup in acl.AccessControlListGroups)
		{
			bool valid = await db.Set<Group>()
				.Where(x => x.Id == aclGroup.GroupId)
				.Where(x => x.CompanyId == acl.CompanyId)
				.Where(x => x.Active)
				.AnyAsync(cancellationToken);

			if (!valid)
			{
				_ = db.Remove(aclGroup);
				continue;
			}

			effectiveUsers.UnionWith(
				await db.Set<GroupUser>()
					.Where(x => x.GroupId == aclGroup.GroupId)
					.Where(x => x.Active)
					.Where(x => x.User.Status != Status.DELETED)
					.Select(x => x.UserId)
					.ToListAsync(cancellationToken)
			);
		}

		// Check Jobs
		foreach (AccessControlListJob aclJob in acl.AccessControlListJobs)
		{
			bool valid = await db.Set<Job>()
				.Where(x => x.Id == aclJob.Id)
				.Where(x => x.CompanyId == acl.CompanyId)
				.Where(x => x.Status != Status.DELETED)
				.AnyAsync(cancellationToken);

			if (!valid)
			{
				_ = db.Remove(aclJob);
				continue;
			}

			effectiveUsers.UnionWith(
				await db.Set<JobUser>()
					.Where(x => x.JobId == aclJob.Id)
					.Where(x => x.Active)
					.Where(x => x.User.Status != Status.DELETED)
					.Select(x => x.UserId)
					.ToListAsync(cancellationToken)
			);

			effectiveUsers.UnionWith(
				await db.Set<GroupUser>()
					.Where(x => x.Group.Active)
					.Where(x => x.Group.JobGroups.Where(y => y.JobId == aclJob.Id).Any())
					.Where(x => x.Active)
					.Where(x => x.User.Status != Status.DELETED)
					.Select(x => x.UserId)
					.ToListAsync(cancellationToken)
			);
		}

		// Re-compute effective users
		foreach (int effectiveUserId in effectiveUsers)
		{
			_ = db.Add(new AccessControlListEffectiveUser() { AccessControlList = acl, UserId = effectiveUserId, });
		}

		_ = await db.SaveChangesAsync(cancellationToken);
	}
}
