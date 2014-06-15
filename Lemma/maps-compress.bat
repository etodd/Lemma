cd Game
gzip *.map
FOR %%f IN (*.map.gz) DO RENAME "%%f" "%%~nf"
cd Challenge
gzip *.map
FOR %%f IN (*.map.gz) DO RENAME "%%f" "%%~nf"
cd ../../Content
gzip *.map
FOR %%f IN (*.map.gz) DO RENAME "%%f" "%%~nf"
cd ..