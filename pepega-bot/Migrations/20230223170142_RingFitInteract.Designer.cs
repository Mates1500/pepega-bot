﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pepega_bot.Module;

namespace pepega_bot.Migrations
{
    [DbContext(typeof(ResultDatabaseContext))]
    [Migration("20230223170142_RingFitInteract")]
    partial class RingFitInteract
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "3.1.0");

            modelBuilder.Entity("pepega_bot.Database.EmoteStatMatch", b =>
                {
                    b.Property<uint>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("MatchesCount")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("MessageLength")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("TimestampUtc")
                        .HasColumnType("TEXT");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("EmoteStatMatches");
                });

            modelBuilder.Entity("pepega_bot.Module.DbWordEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Count")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Value")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("WordEntries");
                });

            modelBuilder.Entity("pepega_bot.Module.RingFitReact", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmoteId")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsApproximateValue")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("MessageTime")
                        .HasColumnType("TEXT");

                    b.Property<uint>("MinuteValue")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("RingFitReacts");
                });
#pragma warning restore 612, 618
        }
    }
}
