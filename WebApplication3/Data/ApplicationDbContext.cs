﻿﻿using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<SingleChoiceQuestion> SingleChoiceQuestions { get; set; }
        public DbSet<MultiChoiceQuestion> MultiChoiceQuestions { get; set; }
        public DbSet<TextQuestion> TextQuestions { get; set; }
        public DbSet<Option> Options { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Question>(q =>
            {
                q.HasDiscriminator<string>("QuestionType");
                q.ToTable("Question");
                q.Property(e => e.QuestionType)
                    .HasMaxLength(50).HasColumnName("question_type");
            });
            builder.Entity<MultiChoiceQuestion>().ToTable("MultiChoiceQuestion");
            builder.Entity<SingleChoiceQuestion>().ToTable("SingleChoiceQuestion");
            builder.Entity<TextQuestion>().ToTable("TextQuestion");
            builder.Entity<Option>().ToTable("Option");
            builder.Entity<Test>().ToTable("Test");
            builder.Entity<TestResult>().ToTable("TestResult");
            builder.Entity<User>().ToTable("User");
            
            
        }

    }
}
