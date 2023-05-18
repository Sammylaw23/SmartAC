﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Theoremone.SmartAc.Infrastructure.DbContexts;

#nullable disable

namespace Theoremone.SmartAc.Infrastructure.Migrations
{
    [DbContext(typeof(SmartAcContext))]
    [Migration("20230428215713_InitialMigration")]
    partial class InitialMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.3");

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.Device", b =>
                {
                    b.Property<string>("SerialNumber")
                        .HasMaxLength(32)
                        .HasColumnType("TEXT");

                    b.Property<string>("FirmwareVersion")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FirstRegistrationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastRegistrationDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("SharedSecret")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("TEXT");

                    b.HasKey("SerialNumber");

                    b.ToTable("Devices");
                });

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.DeviceAlert", b =>
                {
                    b.Property<int>("DeviceAlertId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<double>("Data")
                        .HasColumnType("REAL");

                    b.Property<string>("DataNonNumeric")
                        .HasColumnType("TEXT");

                    b.Property<string>("DateCreated")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DateLastRecorded")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DateRecorded")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("DeviceSerialNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("State")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("DeviceAlertId");

                    b.ToTable("DeviceAlerts");
                });

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.DeviceReading", b =>
                {
                    b.Property<int>("DeviceReadingId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<double>("CarbonMonoxide")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<string>("DeviceSerialNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Health")
                        .HasColumnType("INTEGER");

                    b.Property<double>("Humidity")
                        .HasColumnType("decimal(5, 2)");

                    b.Property<string>("ReceivedDateTime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RecordedDateTime")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<double>("Temperature")
                        .HasColumnType("decimal(5, 2)");

                    b.HasKey("DeviceReadingId");

                    b.HasIndex("DeviceSerialNumber");

                    b.ToTable("DeviceReadings");
                });

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.DeviceRegistration", b =>
                {
                    b.Property<int>("DeviceRegistrationId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("Active")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DeviceSerialNumber")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("RegistrationDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("TokenId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("DeviceRegistrationId");

                    b.HasIndex("DeviceSerialNumber");

                    b.ToTable("DeviceRegistrations");
                });

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.DeviceReading", b =>
                {
                    b.HasOne("Theoremone.SmartAc.Domain.Entities.Device", "Device")
                        .WithMany()
                        .HasForeignKey("DeviceSerialNumber")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");
                });

            modelBuilder.Entity("Theoremone.SmartAc.Domain.Entities.DeviceRegistration", b =>
                {
                    b.HasOne("Theoremone.SmartAc.Domain.Entities.Device", "Device")
                        .WithMany()
                        .HasForeignKey("DeviceSerialNumber")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Device");
                });
#pragma warning restore 612, 618
        }
    }
}
