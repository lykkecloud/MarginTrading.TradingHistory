## 1.14.1 (May 11, 2020)

* BUGS-1635 (TBD): Trades duplication

The following sql script has to be executed against database:

```sql
-- Update Trades table
with cte as (
    select OID,
           Id,
           AccountId,
           AssetPairId,
           TradeTimestamp,
           Volume,
           ROW_NUMBER()
                   over (partition by Id, AccountId, AssetPairId, TradeTimestamp, Volume order by Id, AccountId, AssetPairId, TradeTimestamp, Volume) row_num
    from Trades)
delete
from cte
where row_num > 1
go


create unique index IX_Trades_Id_AccountId_AssetPairId_TradeTimestamp_Volume
	on Trades (Id, AccountId, AssetPairId, TradeTimestamp, Volume)
go

-- Update Deals table
with cte as (
    select OID,
           DealId,
           AccountId,
           AssetPairId,
           Direction,
           Volume,
           Created,
           ROW_NUMBER()
                   over (partition by DealId, AccountId, AssetPairId, Direction, Volume, Created order by DealId, AccountId, AssetPairId, Direction, Volume, Created) row_num
    from Deals)
delete
from cte
where row_num > 1
go

create unique index IX_Deals_DealId_AccountId_AssetPairId_Direction_Volume_Created
	on Deals (DealId, AccountId, AssetPairId, Direction, Volume, Created)
go

-- Update PositionHistory
with cte as (
    select OID,
           Id,
           DealId,
           AccountId,
           AssetPairId,
           Direction,
           Volume,
           HistoryTimestamp,
           row_number()
                   over (partition by Id, DealId, AccountId, AssetPairId, Direction, Volume, HistoryTimestamp order by Id, DealId, AccountId, AssetPairId, Direction, Volume, HistoryTimestamp) row_num
    from PositionsHistory)
delete
from cte
where row_num > 1
go

create unique index IX_PositionHistory_Id_DealId_AccountId_AssetPairId_Direction_Volume_HistoryTimestamp
	on PositionsHistory (Id, DealId, AccountId, AssetPairId, Direction, Volume, HistoryTimestamp)
go
```