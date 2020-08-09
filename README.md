# AzureStoragePoc

> Strategies :
> - `LegacyCosmosStorage` : based only on cosmos db
> - `SearchCosmosStorage` : based on azure search and cosmos db
> - `BlobCosmosStorage` : based on azure blobs and cosmos db
> - `SoftBlobCosmosStorage` : based on azure blobs and cosmos db (soft documents)
> - `SoftSearchCosmosStorage` : based on azure search, blobs and cosmos db (soft documents)

```
Running strategies with '1000' products [Throughput is configured to '400' RU]

Strategy 'LegacyCosmosDbStorage'

SaveResponse
TotalRequestUnits: 22232.1 RU
DataRequestUnits: 5872 RU
PayloadRequestUnits: 3030.1 RU
CorrelationRequestUnits: 13330 RU

GetResponse
TotalRequestUnits: 12.25 RU
OrderIdResponse: 2.83 RU
DataResponse: 9.42 RU

ElapsedTime: 4788 ms

Strategy 'SearchCosmosDbStorage'

SaveResponse
TotalRequestUnits: 8902.1 RU
DataRequestUnits: 5872 RU
PayloadRequestUnits: 3030.1 RU

GetResponse
TotalRequestUnits: 9.42 RU
DataResponse: 9.42 RU
OrderId: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461
TransactionId: cc09285b-c7b6-457a-aa2e-3fe1990c32f4

ElapsedTime: 1890 ms

Strategy 'BlobCosmosDbStorage'

SaveResponse
TotalRequestUnits: 8902.1 RU
DataRequestUnits: 5872 RU
PayloadRequestUnits: 3030.1 RU
Blobs: 1000 files

GetResponse
TotalRequestUnits: 9.42 RU
DataResponse: 9.42 RU
OrderId: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461
TransactionId: 5a4490d7-1426-4f1d-8dde-e336b1acf740
Found order blob: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461.txt

ElapsedTime: 7537 ms

Strategy 'SoftBlobCosmosDbStorage'

SaveResponse
TotalRequestUnits: 4367.24 RU
DataRequestUnits: 2183.62 RU
PayloadRequestUnits: 2183.62 RU
Blobs: 1002 uploaded

GetResponse
TotalRequestUnits: 5.49 RU
DataResponse: 5.49 RU
OrderId: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461
TransactionId: fae99b9c-4806-4721-a926-e4a0814cf9e2
OrderPrice: 311.667612883107320
Found order blob: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461.txt

ElapsedTime: 7851 ms

Strategy 'SoftSearchCosmosDbStorage'

SaveResponse
TotalRequestUnits: 4367.24 RU
DataRequestUnits: 2183.62 RU
PayloadRequestUnits: 2183.62 RU
Blobs: 2 uploaded

GetResponse
TotalRequestUnits: 5.49 RU
DataResponse: 5.49 RU
OrderId: 4d6d7ba0-3b4f-4cd7-8653-9da40c000461
TransactionId: 2f1d92d0-55cd-427c-b795-a47c9579cc5a
Found data blob: 2f1d92d0-55cd-427c-b795-a47c9579cc5a.txt

ElapsedTime: 1177 ms
```
