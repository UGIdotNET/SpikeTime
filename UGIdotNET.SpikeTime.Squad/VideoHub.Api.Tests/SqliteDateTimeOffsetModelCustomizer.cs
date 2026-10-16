using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace VideoHub.Api.Tests;

// SQLite cannot ORDER BY / compare DateTimeOffset natively: store it as a number so the real queries run unchanged.
public class SqliteDateTimeOffsetModelCustomizer(ModelCustomizerDependencies dependencies) : ModelCustomizer(dependencies)
{
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
        {
            property.SetValueConverter(new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks, v => new DateTimeOffset(v, TimeSpan.Zero)));
        }
    }
}
