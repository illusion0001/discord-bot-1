﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CompatApiClient;
using Microsoft.EntityFrameworkCore;

namespace CompatBot.Database
{
    internal class ThumbnailDb: DbContext
    {
        public DbSet<State> State { get; set; }
        public DbSet<Thumbnail> Thumbnail { get; set; }
        public DbSet<SyscallInfo> SyscallInfo { get; set; }
        public DbSet<SyscallToProductMap> SyscallToProductMap { get; set; }
        public DbSet<Metacritic> Metacritic { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var dbPath = DbImporter.GetDbPath("thumbs.db", Environment.SpecialFolder.LocalApplicationData);
#if DEBUG
            optionsBuilder.UseLoggerFactory(Config.LoggerFactory);
#endif
            optionsBuilder.UseSqlite($"Data Source=\"{dbPath}\"");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //configure indices
            modelBuilder.Entity<State>().HasIndex(s => s.Locale).IsUnique().HasName("state_locale");
            modelBuilder.Entity<State>().HasIndex(s => s.Timestamp).HasName("state_timestamp");
            modelBuilder.Entity<Thumbnail>().HasIndex(m => m.ProductCode).IsUnique().HasName("thumbnail_product_code");
            modelBuilder.Entity<Thumbnail>().HasIndex(m => m.ContentId).IsUnique().HasName("thumbnail_content_id");
            modelBuilder.Entity<Thumbnail>().HasIndex(m => m.Timestamp).HasName("thumbnail_timestamp");
            modelBuilder.Entity<SyscallInfo>().HasIndex(sci => sci.Function).HasName("syscall_info_function");
            modelBuilder.Entity<SyscallToProductMap>().HasKey(m => new {m.ProductId, m.SyscallInfoId});

            //configure default policy of Id being the primary key
            modelBuilder.ConfigureDefaultPkConvention();

            //configure name conversion for all configured entities from CamelCase to snake_case
            modelBuilder.ConfigureMapping(NamingStyles.Underscore);
        }
    }

    internal class State
    {
        public int Id { get; set; }
        public string Locale { get; set; }
        public long Timestamp { get; set; }
    }

    internal class Thumbnail
    {
        public int Id { get; set; }
        [Required]
        public string ProductCode { get; set; }
        public string ContentId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string EmbeddableUrl { get; set; }
        public long Timestamp { get; set; }
        public int? EmbedColor { get; set; }
        public CompatStatus? CompatibilityStatus { get; set; }
        public long? CompatibilityChangeDate { get; set; }

        public int? MetacriticId { get; set; }
        public Metacritic Metacritic { get; set; }

        public List<SyscallToProductMap> SyscallToProductMap { get; set; }
    }

    public enum CompatStatus : byte
    {
        Unknown = 0,
        Nothing = 10,
        Loadable = 20,
        Intro = 30,
        Ingame = 40,
        Playable = 50,
    }

    internal class SyscallInfo
    {
        public int Id { get; set; }
        [Required]
        public string Function { get; set; }

        public List<SyscallToProductMap> SyscallToProductMap { get; set; }
    }

    internal class SyscallToProductMap
    {
        public int ProductId { get; set; }
        public Thumbnail Product { get; set; }

        public int SyscallInfoId { get; set; }
        public SyscallInfo SyscallInfo { get; set; }
    }

    internal class Metacritic
    {
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        public byte? CriticScore { get; set; }
        public byte? UserScore { get; set; }
        public string Notes { get; set; }

        public Metacritic WithTitle(string title)
        {
            return new Metacritic
            {
                Title = title,
                CriticScore = CriticScore,
                UserScore = UserScore,
                Notes = Notes,
            };
        }
    }
}
