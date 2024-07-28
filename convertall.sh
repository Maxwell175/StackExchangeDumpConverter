#!/usr/bin/env bash

# This is a sample script to loop over all dumps except SO and run the converter against them.

DUMP_DIR=$(pwd)
DUMP_CONVERTER=$(pwd)/bin/Release/net8.0/StackExchangeDumpConverter

JOBS=4

for f in $DUMP_DIR/*.7z; do
  file_name=$(basename ${f})
  site_name=${file_name/.7z/}
  site_name=${site_name/.stackexchange.com/}
  site_name=${site_name/.com/}
  if [[ $site_name == "stackoverflow"* ]]; then
    # We will do SO separately.
    continue
  fi
  
  site_name=${site_name/./_}
  
  (
    $DUMP_CONVERTER \
      -d postgres --postgres-user testuser --postgres-pass testpass \
      --postgres-db $site_name --postgres-replace true --postgres-batch-size 1000000 $f
      
    PGPASSWORD="testpass" pg_dump -j 8 -Fd -f $DUMP_DIR/results/$site_name -U testuser $site_name
      
    echo $site_name 
  )&
  
  if [[ $(jobs -r -p | wc -l) -ge $JOBS ]]; then wait -n; fi
done

wait
