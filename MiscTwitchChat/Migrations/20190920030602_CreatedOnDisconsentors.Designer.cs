﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MiscTwitchChat;

namespace MiscTwitchChat.Migrations
{
    [DbContext(typeof(MiscTwitchDbContext))]
    [Migration("20190920030602_CreatedOnDisconsentors")]
    partial class CreatedOnDisconsentors
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("MiscTwitchChat.Disconsenter", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTimeOffset>("Created");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Disconsenters");
                });
#pragma warning restore 612, 618
        }
    }
}
