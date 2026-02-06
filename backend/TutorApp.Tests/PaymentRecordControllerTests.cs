using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TutorApp.API.Controllers;
using TutorApp.API.Data;
using TutorApp.API.DTOs;
using TutorApp.API.Models;
using Xunit;

namespace TutorApp.Tests
{
    public class PaymentRecordControllerTests
    {
        private TutorDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<TutorDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new TutorDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private PaymentRecordController GetController(TutorDbContext context, string username)
        {
            var controller = new PaymentRecordController(context);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, username),
            }, "mock"));

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };

            return controller;
        }

        private async Task SeedData(TutorDbContext context)
        {
            context.Account.Add(new Account { Username = "tutor1", IsTutor = true, DisplayName = "Tutor One", Email = "tutor1@example.com" });
            context.Account.Add(new Account { Username = "tutor2", IsTutor = true, DisplayName = "Tutor Two", Email = "tutor2@example.com" });

            context.Account.Add(new Account { Username = "student1", IsTutor = false, DisplayName = "Student One", Email = "student1@example.com" });
            context.Account.Add(new Account { Username = "student2", IsTutor = false, DisplayName = "Student Two", Email = "student2@example.com" });

            context.PaymentRecord.Add(new PaymentRecord
            {
                PaymentRecordID = 1,
                TutorUsername = "tutor1",
                StudentUsername = "student1",
                AmountPaid = 100,
                MeansOfPayment = MeansOfPayment.Cash,
                PaidOn = DateTime.Now
            });

            context.PaymentRecord.Add(new PaymentRecord
            {
                PaymentRecordID = 2,
                TutorUsername = "tutor2",
                StudentUsername = "student2",
                AmountPaid = 200,
                MeansOfPayment = MeansOfPayment.BankTransfer,
                PaidOn = DateTime.Now
            });

            await context.SaveChangesAsync();
        }

        // Checks that a tutor can create a payment record for themself.
        [Fact]
        public async Task PostPaymentRecord_Tutor_CanCreateForOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newRecord = new PaymentRecordCreateDto
            {
                TutorUsername = "tutor1",
                StudentUsername = "student1",
                AmountPaid = 150,
                MeansOfPayment = MeansOfPayment.BLIK,
                PaidOn = DateTime.Now
            };

            var result = await controller.PostPaymentRecord(newRecord);

            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdRecord = Assert.IsType<PaymentRecordDto>(createdAtActionResult.Value);
            Assert.Equal("tutor1", createdRecord.TutorUsername);
        }

        // Checks that a Tutor cannot create a payment record for another Tutor.
        [Fact]
        public async Task PostPaymentRecord_Tutor_CannotCreateForOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var newRecord = new PaymentRecordCreateDto
            {
                TutorUsername = "tutor2",
                StudentUsername = "student1",
                AmountPaid = 150,
                MeansOfPayment = MeansOfPayment.BLIK,
                PaidOn = DateTime.Now
            };

            var result = await controller.PostPaymentRecord(newRecord);

            Assert.IsType<BadRequestResult>(result.Result);
        }

        // Checks that a Student cannot create a payment record.
        [Fact]
        public async Task PostPaymentRecord_Student_CannotCreate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var newRecord = new PaymentRecordCreateDto
            {
                TutorUsername = "tutor1",
                StudentUsername = "student1",
                AmountPaid = 150,
                MeansOfPayment = MeansOfPayment.BLIK,
                PaidOn = DateTime.Now
            };

            var result = await controller.PostPaymentRecord(newRecord);

            Assert.IsType<ForbidResult>(result.Result);
        }

        // Checks that a Tutor can update their own payment record.
        [Fact]
        public async Task PutPaymentRecord_Tutor_CanUpdateOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var record = await context.PaymentRecord.FindAsync(1);
            record.AmountPaid = 120;

            var result = await controller.PutPaymentRecord(1, record);

            Assert.IsType<NoContentResult>(result);
            var updatedRecord = await context.PaymentRecord.FindAsync(1);
            Assert.Equal(120, updatedRecord.AmountPaid);
        }

        // Checks that a Tutor cannot update a payment record for another tutor.
        [Fact]
        public async Task PutPaymentRecord_Tutor_CannotUpdateOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var record = await context.PaymentRecord.FindAsync(2);
            record.AmountPaid = 250;

            var result = await controller.PutPaymentRecord(2, record);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Student cannot update a payment record.
        [Fact]
        public async Task PutPaymentRecord_Student_CannotUpdate()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var record = await context.PaymentRecord.FindAsync(1);
            record.AmountPaid = 999;

            var result = await controller.PutPaymentRecord(1, record);

            // Expect Forbid (403)
            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Tutor can delete their own payment record.
        [Fact]
        public async Task DeletePaymentRecord_Tutor_CanDeleteOwn()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.DeletePaymentRecord(1);

            Assert.IsType<NoContentResult>(result);
            var deletedRecord = await context.PaymentRecord.FindAsync(1);
            Assert.Null(deletedRecord);
        }

        // Checks that a Tutor cannot delete another tutor's payment record.
        [Fact]
        public async Task DeletePaymentRecord_Tutor_CannotDeleteOther()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "tutor1");

            var result = await controller.DeletePaymentRecord(2);

            Assert.IsType<ForbidResult>(result);
        }

        // Checks that a Student cannot delete a payment record.
        [Fact]
        public async Task DeletePaymentRecord_Student_CannotDelete()
        {
            using var context = GetDatabaseContext();
            await SeedData(context);
            var controller = GetController(context, "student1");

            var result = await controller.DeletePaymentRecord(1);

            Assert.IsType<ForbidResult>(result);
        }
    }
}
