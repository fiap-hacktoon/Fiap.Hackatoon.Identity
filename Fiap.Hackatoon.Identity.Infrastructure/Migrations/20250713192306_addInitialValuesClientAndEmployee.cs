using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fiap.Hackatoon.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class addInitialValuesClientAndEmployee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("insert into Employees values (1,'rodrigo','employee@gmail.com','123456789',getdate())");
            migrationBuilder.Sql("insert into Clients values('33333333303',0,'ricardo','client@gmail.com','123456789',getdate(),'1987-12-30')");


            //insert into Employees values (1,'rodrigo','rmahlow@gmail.com','123456789',getdate())
            //insert into Clients values('33333333303',0,'ricardo','r@gmail.com','123456789',getdate(),'1987-12-30')
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
