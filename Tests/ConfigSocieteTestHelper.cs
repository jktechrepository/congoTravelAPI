using CongoTravel.Data;
using CongoTravel.Helpers;
using CongoTravel.Models;
using CongoTravel.Services;
using Microsoft.EntityFrameworkCore;

namespace CongoTravel.Tests
{
    public static class ConfigSocieteTestHelper
    {
        public static ConfigSocieteService Create(CongoTravelDbContext ctx) => new(ctx);

        public static async Task<ConfigSociete> SeedAsync(
            CongoTravelDbContext ctx,
            int idSociete,
            Action<ConfigSociete>? configure = null)
        {
            if (await ctx.ConfigSocietes.AnyAsync(c => c.IdSociete == idSociete))
            {
                var existing = await ctx.ConfigSocietes.FirstAsync(c => c.IdSociete == idSociete);
                configure?.Invoke(existing);
                await ctx.SaveChangesAsync();
                return existing;
            }

            var config = ConfigSocieteDefaults.CreateForSociete(idSociete);
            configure?.Invoke(config);
            ctx.ConfigSocietes.Add(config);
            await ctx.SaveChangesAsync();
            return config;
        }
    }
}
