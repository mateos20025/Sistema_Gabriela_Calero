using Consultorio.Infrastructure.Identidad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Consultorio.Infrastructure.Persistencia.Configuraciones;

// Identity crea por defecto tablas "AspNetUsers", "AspNetRoles"...
// Aquí les damos nombres en español; las columnas pasan a snake_case automáticamente.

internal sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.Property(u => u.NombreCompleto).HasMaxLength(100).IsRequired();
    }
}

internal sealed class RolConfiguracion : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder) => builder.ToTable("roles");
}

internal sealed class UsuarioRolConfiguracion : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("usuario_roles");
}

internal sealed class UsuarioClaimConfiguracion : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("usuario_claims");
}

internal sealed class UsuarioLoginConfiguracion : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("usuario_logins");
}

internal sealed class UsuarioTokenConfiguracion : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("usuario_tokens");
}

internal sealed class RolClaimConfiguracion : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTable("rol_claims");
}
