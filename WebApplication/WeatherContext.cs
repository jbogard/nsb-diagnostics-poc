using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WebApplication;

public class WeatherContext : DbContext
{
    public WeatherContext(DbContextOptions<WeatherContext> options) : base(options)
    {
    }

    public DbSet<WeatherForecast> Forecasts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WeatherForecast>().ToTable("Weather");
    }
}

public static class DbInitializer
{
    private static readonly string[] Summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public static void Initialize(WeatherContext context)
    {
        context.Database.EnsureCreated();

        if (context.Forecasts.Any())
        {
            return;
        }

        var rng = new Random();
        var weathers = Enumerable.Range(1, 20).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();

        context.Forecasts.AddRange(weathers);

        context.SaveChanges();
    }
}