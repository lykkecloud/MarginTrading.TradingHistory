CREATE OR ALTER PROCEDURE [dbo].[SP_InsertDeal](@DealId [nvarchar](64),
                                                @Created [datetime],
                                                @AccountId [nvarchar](64),
                                                @AssetPairId [nvarchar](64),
                                                @OpenTradeId [nvarchar](64),
                                                @OpenOrderType [nvarchar](64),
                                                @OpenOrderVolume [float],
                                                @OpenOrderExpectedPrice [float],
                                                @CloseTradeId [nvarchar](64),
                                                @CloseOrderType [nvarchar](64),
                                                @CloseOrderVolume [float],
                                                @CloseOrderExpectedPrice [float],
                                                @Direction [nvarchar](64),
                                                @Volume [float],
                                                @Originator [nvarchar](64),
                                                @OpenPrice [float],
                                                @OpenFxPrice [float],
                                                @ClosePrice [float],
                                                @CloseFxPrice [float],
                                                @Fpl [float],
                                                @PnlOfTheLastDay [float],
                                                @AdditionalInfo [nvarchar](MAX),
                                                @PositionId [nvarchar](64))
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO [dbo].[Deals]
    (DealId, Created, AccountId, AssetPairId, OpenTradeId, OpenOrderType, OpenOrderVolume, OpenOrderExpectedPrice,
     CloseTradeId, CloseOrderType, CloseOrderVolume, CloseOrderExpectedPrice, Direction, Volume, Originator,
     OpenPrice, OpenFxPrice, ClosePrice, CloseFxPrice, Fpl, PnlOfTheLastDay, AdditionalInfo)
    VALUES (@DealId, @Created, @AccountId, @AssetPairId, @OpenTradeId, @OpenOrderType, @OpenOrderVolume,
            @OpenOrderExpectedPrice, @CloseTradeId, @CloseOrderType, @CloseOrderVolume, @CloseOrderExpectedPrice,
            @Direction, @Volume, @Originator, @OpenPrice, @OpenFxPrice, @ClosePrice, @CloseFxPrice, @Fpl,
            @PnlOfTheLastDay, @AdditionalInfo);

    WITH selectedAccounts AS
             (
                 SELECT account.EventSourceId,
                        account.ReasonType,
                        account.ChangeAmount
                 FROM dbo.[Deals] AS deal,
                      dbo.AccountHistory AS account
                 WHERE (account.EventSourceId IN (deal.OpenTradeId, deal.CloseTradeId))
             )
    UPDATE [dbo].[Deals]
    SET [OvernightFees] =
            (SELECT CONVERT(DECIMAL(24, 13), Sum(swapHistory.SwapValue / ABS(swapHistory.Volume)) * ABS(deal.Volume))
             FROM dbo.[Deals] AS deal,
                  dbo.PositionsHistory AS position,
                  dbo.OvernightSwapHistory AS swapHistory
             WHERE position.Id = @PositionId
               AND deal.DealId = position.DealId
               AND position.Id = swapHistory.PositionId
               AND swapHistory.IsSuccess = 1
             GROUP BY deal.DealId, ABS(deal.Volume)
            ),
        [Commission]    = (
            SELECT CONVERT(DECIMAL(24, 13),
                           ((ISNULL(openingCommission.ChangeAmount, 0.0) / ABS(deal.OpenOrderVolume)
                               + ISNULL(closingCommission.ChangeAmount, 0.0) / ABS(deal.CloseOrderVolume))
                               * ABS(deal.Volume)))
            FROM dbo.[Deals] AS deal
                     INNER JOIN selectedAccounts openingCommission
                                ON deal.OpenTradeId = openingCommission.EventSourceId AND
                                   openingCommission.ReasonType = 'Commission'
                     LEFT OUTER JOIN selectedAccounts closingCommission
                                     ON deal.CloseTradeId = closingCommission.EventSourceId AND
                                        closingCommission.ReasonType = 'Commission'
            WHERE deal.DealId = @DealId
        ),
        [OnBehalfFee]   = (
            SELECT CONVERT(DECIMAL(24, 13),
                           ((ISNULL(openingOnBehalf.ChangeAmount, 0.0) / ABS(deal.OpenOrderVolume)
                               + ISNULL(closingOnBehalf.ChangeAmount, 0.0) / ABS(deal.CloseOrderVolume))
                               * ABS(deal.Volume)))
            FROM [dbo].[Deals] deal
                     LEFT OUTER JOIN selectedAccounts openingOnBehalf
                                     ON deal.OpenTradeId = openingOnBehalf.EventSourceId AND
                                        openingOnBehalf.ReasonType = 'OnBehalf'
                     LEFT OUTER JOIN selectedAccounts closingOnBehalf
                                     ON deal.CloseTradeId = closingOnBehalf.EventSourceId AND
                                        closingOnBehalf.ReasonType = 'OnBehalf'
            WHERE deal.DealId = @DealId
        ),
        [Taxes]         = (
            SELECT CONVERT(DECIMAL(24, 13), ISNULL(account.ChangeAmount, 0.0))
            FROM [dbo].[Deals] deal,
                 [dbo].[AccountHistory] account
            WHERE account.EventSourceId = deal.DealId
              AND account.ReasonType = 'Tax'
              AND deal.DealId = @DealId -- it could also be CompensationId, so it is automatically skipped
        )
    WHERE [Deals].DealId = @DealId;

END;