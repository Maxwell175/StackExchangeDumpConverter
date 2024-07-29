# StackExchange Dump Converter

This is a tool that takes a StackExchange site data dump and imports it to various destinations.

## Destinations

All relational database destinations include foreign keys.

* Relational Databases
  * PostgreSQL
  * SQL Server
  * SQLite

## Data Fixes

This tool detects references to missing Posts or Users and adds dummy records to compensate.

## Compiling

1. Make sure you have .NET 8.0 SDK installed.
2. Run `dotnet build` at the root of this repo.

## How to Use

See the help output by running the tool with the `-h` option.

Here is a sample command to import the dump of the Unix SE site to a local PostgreSQL instance with a large batch size:

```shell
./bin/Release/net8.0/StackExchangeDumpConverter \                  
    -d postgres --postgres-user testuser --postgres-pass testpass \
    --postgres-db unix --postgres-replace true --postgres-batch-size 1000000 unix.stackexchange.com.7z
```

There is also a sample `convertall.sh` file that the reader can adapt to convert all 
data dumps in an automated manner.

## Performance

To improve the performance of the load, it is recommended to increase the batch size. However this comes at 
a cost of increased memory usage.

Loading the StackOverflow data dump using a batch size of 1000000 takes 12 hr 6 min with a peak memory usage of 7.7GB.

## Pre-converted files

This is a list of magnet links to torrent downloads for pre-converted dumps using this tool.

* April 2024 Data Dump
  * PostgreSQL: `magnet:?xt=urn:btih:f3af0353b052ca46d9e77825595cdcf577c2f51c&dn=stackexchange_postgresql&tr=http%3A%2F%2Fbt1.archive.org%3A6969%2Fannounce&tr=http%3A%2F%2Fbt2.archive.org%3A6969%2Fannounce`
