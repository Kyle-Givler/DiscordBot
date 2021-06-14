﻿// <auto-generated />
using System;
using DiscordBotApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DiscordBotApi.Migrations
{
    [DbContext(typeof(DiscordBotContext))]
    partial class DiscordBotContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.7")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("DiscordBotApi.Models.Channel", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("ChannelId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("ChannelName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Channel");
                });

            modelBuilder.Entity("DiscordBotApi.Models.Guild", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<decimal>("GuildId")
                        .HasColumnType("decimal(20,0)");

                    b.Property<string>("GuildName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.ToTable("Guild");
                });

            modelBuilder.Entity("DiscordBotApi.Models.ServerLogItem", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

                    b.Property<int?>("ChannelId")
                        .HasColumnType("int");

                    b.Property<string>("Description")
                        .HasMaxLength(2000)
                        .HasColumnType("nvarchar(2000)");

                    b.Property<int?>("GuildId")
                        .HasColumnType("int");

                    b.Property<string>("ThumbnailUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Title")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("ChannelId");

                    b.HasIndex("GuildId");

                    b.ToTable("ServerLogItems");
                });

            modelBuilder.Entity("DiscordBotApi.Models.ServerLogItem", b =>
                {
                    b.HasOne("DiscordBotApi.Models.Channel", "Channel")
                        .WithMany()
                        .HasForeignKey("ChannelId");

                    b.HasOne("DiscordBotApi.Models.Guild", "Guild")
                        .WithMany()
                        .HasForeignKey("GuildId");

                    b.Navigation("Channel");

                    b.Navigation("Guild");
                });
#pragma warning restore 612, 618
        }
    }
}
