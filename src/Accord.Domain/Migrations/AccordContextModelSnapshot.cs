﻿// <auto-generated />
using System;
using Accord.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Accord.Domain.Migrations
{
    [DbContext(typeof(AccordContext))]
    partial class AccordContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.6")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Accord.Domain.Model.ChannelFlag", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("DiscordChannelId", "Type")
                        .IsUnique();

                    b.ToTable("ChannelFlags");
                });

            modelBuilder.Entity("Accord.Domain.Model.NamePattern", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("AddedByUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("AddedDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("OnDiscovery")
                        .HasColumnType("int");

                    b.Property<string>("Pattern")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AddedByUserId");

                    b.ToTable("NamePatterns");
                });

            modelBuilder.Entity("Accord.Domain.Model.Permission", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Permissions");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Permission");
                });

            modelBuilder.Entity("Accord.Domain.Model.RunOption", b =>
                {
                    b.Property<int>("Type")
                        .HasColumnType("int");

                    b.Property<string>("Value")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Type");

                    b.ToTable("RunOptions");

                    b.HasData(
                        new
                        {
                            Type = 0,
                            Value = "False"
                        },
                        new
                        {
                            Type = 1,
                            Value = "False"
                        },
                        new
                        {
                            Type = 2,
                            Value = "10"
                        },
                        new
                        {
                            Type = 3,
                            Value = "False"
                        },
                        new
                        {
                            Type = 4,
                            Value = ""
                        },
                        new
                        {
                            Type = 5,
                            Value = ""
                        },
                        new
                        {
                            Type = 6,
                            Value = ""
                        },
                        new
                        {
                            Type = 8,
                            Value = "False"
                        },
                        new
                        {
                            Type = 7,
                            Value = "3"
                        });
                });

            modelBuilder.Entity("Accord.Domain.Model.User", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("FirstSeenDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset?>("JoinedGuildDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<DateTimeOffset>("LastSeenDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<string>("Nickname")
                        .HasColumnType("nvarchar(max)");

                    b.Property<double>("ParticipationPercentile")
                        .HasColumnType("float");

                    b.Property<int>("ParticipationPoints")
                        .HasColumnType("int");

                    b.Property<int>("ParticipationRank")
                        .HasColumnType("int");

                    b.Property<string>("UsernameWithDiscriminator")
                        .HasColumnType("nvarchar(max)");

                    b.Property<float>("Xp")
                        .HasColumnType("real");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserHiddenChannel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal?>("ParentDiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserHiddenChannels");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserMessage", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("SentDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserMessages");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReminder", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("RemindAt")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserReminders");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReport", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal?>("ClosedByUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset?>("ClosedDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("InboxDiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("OpenedByUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("OpenedDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("OutboxDiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("ClosedByUserId");

                    b.HasIndex("OpenedByUserId");

                    b.ToTable("UserReports");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReportBlock", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("BlockedByUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<DateTimeOffset>("BlockedDateTime")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.HasIndex("BlockedByUserId");

                    b.ToTable("UserReportBlocks");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReportMessage", b =>
                {
                    b.Property<decimal>("Id")
                        .HasColumnType("decimal(20,0)");

                    b.Property<decimal>("AuthorUserId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<bool>("IsInternal")
                        .HasColumnType("bit");

                    b.Property<DateTimeOffset>("SentDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<int>("UserReportId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("AuthorUserId");

                    b.HasIndex("UserReportId");

                    b.ToTable("UserReportMessages");
                });

            modelBuilder.Entity("Accord.Domain.Model.VoiceSession", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("DiscordChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("DiscordSessionId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset?>("EndDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<bool>("HasBeenCountedToXp")
                        .HasColumnType("bit");

                    b.Property<double?>("MinutesInVoiceChannel")
                        .HasColumnType("float");

                    b.Property<DateTimeOffset>("StartDateTime")
                        .HasColumnType("datetimeoffset");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("VoiceConnections");
                });

            modelBuilder.Entity("Accord.Domain.Model.RolePermission", b =>
                {
                    b.HasBaseType("Accord.Domain.Model.Permission");

                    b.Property<decimal>("RoleId")
                        .HasColumnType("decimal(20,0)");

                    b.HasIndex("RoleId", "Type")
                        .IsUnique()
                        .HasFilter("[RoleId] IS NOT NULL");

                    b.HasDiscriminator().HasValue("RolePermission");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserPermission", b =>
                {
                    b.HasBaseType("Accord.Domain.Model.Permission");

                    b.Property<decimal>("UserId")
                        .HasColumnType("decimal(20,0)");

                    b.HasIndex("UserId", "Type")
                        .IsUnique()
                        .HasFilter("[UserId] IS NOT NULL");

                    b.HasDiscriminator().HasValue("UserPermission");
                });

            modelBuilder.Entity("Accord.Domain.Model.NamePattern", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "AddedByUser")
                        .WithMany()
                        .HasForeignKey("AddedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AddedByUser");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserHiddenChannel", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserMessage", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "User")
                        .WithMany("Messages")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReminder", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReport", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "ClosedByUser")
                        .WithMany()
                        .HasForeignKey("ClosedByUserId");

                    b.HasOne("Accord.Domain.Model.User", "OpenedByUser")
                        .WithMany()
                        .HasForeignKey("OpenedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ClosedByUser");

                    b.Navigation("OpenedByUser");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReportBlock", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "BlockedByUser")
                        .WithMany()
                        .HasForeignKey("BlockedByUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BlockedByUser");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReportMessage", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "AuthorUser")
                        .WithMany()
                        .HasForeignKey("AuthorUserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Accord.Domain.Model.UserReports.UserReport", "UserReport")
                        .WithMany("Messages")
                        .HasForeignKey("UserReportId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AuthorUser");

                    b.Navigation("UserReport");
                });

            modelBuilder.Entity("Accord.Domain.Model.VoiceSession", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserPermission", b =>
                {
                    b.HasOne("Accord.Domain.Model.User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Accord.Domain.Model.User", b =>
                {
                    b.Navigation("Messages");
                });

            modelBuilder.Entity("Accord.Domain.Model.UserReports.UserReport", b =>
                {
                    b.Navigation("Messages");
                });
#pragma warning restore 612, 618
        }
    }
}
