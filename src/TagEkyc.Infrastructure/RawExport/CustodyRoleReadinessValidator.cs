using Microsoft.EntityFrameworkCore;
using TagEkyc.Infrastructure.Persistence;

namespace TagEkyc.Infrastructure.RawExport;

public interface IDurableKeyReadinessValidator
{
    Task ValidateAsync(CancellationToken cancellationToken);
}

public sealed class CustodyRoleReadinessValidator(TagEkycDbContext db) : IDurableKeyReadinessValidator
{
    public static readonly string[] Codes =
    [
        "PROD_RAW_EXPORT_CUSTODY_ROLE_MISSING",
        "PROD_RAW_EXPORT_CUSTODY_ROLE_ATTRIBUTE_INVALID",
        "PROD_RAW_EXPORT_CUSTODY_LOGIN_ATTRIBUTE_INVALID",
        "PROD_RAW_EXPORT_CUSTODY_ROLE_GRANT_INVALID",
        "PROD_RAW_EXPORT_CUSTODY_ROLE_CROSS_MEMBERSHIP",
        "PROD_RAW_EXPORT_CUSTODY_ROLE_SET_ROLE_ENABLED",
    ];

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        var rows = await db.Database.SqlQueryRaw<RoleShape>("""
            SELECT r.rolname AS "Name", r.rolcanlogin AS "CanLogin", r.rolsuper AS "Super",
                   r.rolcreatedb AS "CreateDb", r.rolcreaterole AS "CreateRole",
                   r.rolreplication AS "Replication", r.rolbypassrls AS "BypassRls",
                   r.rolinherit AS "Inherit"
            FROM pg_catalog.pg_roles r
            WHERE r.rolname IN ('tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle',
                                'tagekyc_raw_export_encryptor_login','tagekyc_raw_export_reconciler_login','tagekyc_raw_export_lifecycle_login')
            """).ToListAsync(cancellationToken);
        if (rows.Count != 6) throw new DurableKeyReadinessException(Codes[0]);
        if (rows.Where(x => !x.Name.EndsWith("_login", StringComparison.Ordinal)).Any(x => x.CanLogin || x.Super || x.CreateDb || x.CreateRole || x.Replication || x.BypassRls || !x.Inherit))
            throw new DurableKeyReadinessException(Codes[1]);
        if (rows.Where(x => x.Name.EndsWith("_login", StringComparison.Ordinal)).Any(x => !x.CanLogin || x.Super || x.CreateDb || x.CreateRole || x.Replication || x.BypassRls || !x.Inherit))
            throw new DurableKeyReadinessException(Codes[2]);

        var memberships = await db.Database.SqlQueryRaw<RoleMembership>("""
            SELECT member.rolname AS "Member", role.rolname AS "Role", m.admin_option AS "AdminOption",
                   m.inherit_option AS "InheritOption", m.set_option AS "SetOption"
            FROM pg_catalog.pg_auth_members m
            JOIN pg_catalog.pg_roles member ON member.oid=m.member
            JOIN pg_catalog.pg_roles role ON role.oid=m.roleid
            WHERE member.rolname LIKE 'tagekyc_raw_export_%_login'
               OR role.rolname IN ('tagekyc_raw_export_custody_encryptor','tagekyc_raw_export_reconciler','tagekyc_raw_export_lifecycle')
            """).ToListAsync(cancellationToken);
        var expected = new HashSet<(string,string)>
        {
            ("tagekyc_raw_export_encryptor_login","tagekyc_raw_export_custody_encryptor"),
            ("tagekyc_raw_export_reconciler_login","tagekyc_raw_export_reconciler"),
            ("tagekyc_raw_export_lifecycle_login","tagekyc_raw_export_lifecycle"),
        };
        if (memberships.Count != 3 || memberships.Any(x => !expected.Contains((x.Member,x.Role))))
            throw new DurableKeyReadinessException(Codes[3]);
        if (memberships.Any(x => x.AdminOption)) throw new DurableKeyReadinessException(Codes[4]);
        if (memberships.Any(x => !x.InheritOption || x.SetOption)) throw new DurableKeyReadinessException(Codes[5]);
    }

    private sealed record RoleShape(string Name, bool CanLogin, bool Super, bool CreateDb, bool CreateRole, bool Replication, bool BypassRls, bool Inherit);
    private sealed record RoleMembership(string Member, string Role, bool AdminOption, bool InheritOption, bool SetOption);
}
