using Microsoft.EntityFrameworkCore.Migrations;

namespace BlazorTrader.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TDAOptionQuotes",
                columns: table => new
                {
                    symbol = table.Column<string>(nullable: false),
                    quoteTimeInLong = table.Column<long>(nullable: false),
                    putCall = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    exchangeName = table.Column<string>(nullable: true),
                    bid = table.Column<float>(nullable: false),
                    ask = table.Column<float>(nullable: false),
                    last = table.Column<float>(nullable: false),
                    mark = table.Column<float>(nullable: false),
                    bidSize = table.Column<int>(nullable: false),
                    askSize = table.Column<int>(nullable: false),
                    bidAskSize = table.Column<string>(nullable: true),
                    lastSize = table.Column<int>(nullable: false),
                    highPrice = table.Column<float>(nullable: false),
                    lowPrice = table.Column<float>(nullable: false),
                    openPrice = table.Column<float>(nullable: false),
                    closePrice = table.Column<float>(nullable: false),
                    totalVolume = table.Column<int>(nullable: false),
                    tradeDate = table.Column<long>(nullable: false),
                    tradeTimeInLong = table.Column<long>(nullable: false),
                    netChange = table.Column<float>(nullable: false),
                    volatility = table.Column<float>(nullable: false),
                    delta = table.Column<float>(nullable: false),
                    gamma = table.Column<float>(nullable: false),
                    theta = table.Column<float>(nullable: false),
                    vega = table.Column<float>(nullable: false),
                    rho = table.Column<float>(nullable: false),
                    openInterest = table.Column<int>(nullable: false),
                    timeValue = table.Column<float>(nullable: false),
                    theoreticalOptionValue = table.Column<float>(nullable: false),
                    theoreticalVolatility = table.Column<float>(nullable: false),
                    strikePrice = table.Column<float>(nullable: false),
                    expirationDate = table.Column<long>(nullable: false),
                    daysToExpiration = table.Column<float>(nullable: false),
                    expirationType = table.Column<string>(nullable: true),
                    lastTradingDay = table.Column<long>(nullable: false),
                    multiplier = table.Column<float>(nullable: false),
                    settlementType = table.Column<string>(nullable: true),
                    deliverableNote = table.Column<string>(nullable: true),
                    isIndexOption = table.Column<bool>(nullable: false),
                    percentChange = table.Column<float>(nullable: false),
                    markChange = table.Column<float>(nullable: false),
                    markPercentChange = table.Column<float>(nullable: false),
                    nonStandard = table.Column<bool>(nullable: false),
                    mini = table.Column<bool>(nullable: false),
                    inTheMoney = table.Column<bool>(nullable: false),
                    prem = table.Column<float>(nullable: false),
                    prem2 = table.Column<float>(nullable: false),
                    maxLoss = table.Column<float>(nullable: false),
                    breakeven = table.Column<float>(nullable: false),
                    buyLongStrike = table.Column<float>(nullable: false),
                    collateral = table.Column<float>(nullable: false),
                    credit = table.Column<float>(nullable: false),
                    contracts = table.Column<int>(nullable: false),
                    isChecked = table.Column<bool>(nullable: false),
                    buyOption = table.Column<string>(nullable: true),
                    index = table.Column<int>(nullable: false),
                    isManualContracts = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TDAOptionQuotes", x => new { x.symbol, x.quoteTimeInLong });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TDAOptionQuotes");
        }
    }
}
