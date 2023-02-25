﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using pepega_bot.Database;
using pepega_bot.Module;

#nullable disable

namespace pepega_bot.Migrations
{
    [DbContext(typeof(ResultDatabaseContext))]
    partial class ResultDatabaseContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "5.0.17");

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

                    b.HasIndex("TimestampUtc");

                    b.ToTable("EmoteStatMatches", (string)null);
                });

            modelBuilder.Entity("pepega_bot.Database.RingFit.RingFitMessage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("MessageId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("MessageTime")
                        .HasColumnType("TEXT");

                    b.Property<int>("MessageType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("RingFitMessages", (string)null);
                });

            modelBuilder.Entity("pepega_bot.Database.RingFit.RingFitReact", b =>
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

                    b.HasIndex("MessageTime");

                    b.HasIndex("UserId");

                    b.ToTable("RingFitReacts", (string)null);
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

                    b.HasIndex("Value");

                    b.ToTable("WordEntries", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
