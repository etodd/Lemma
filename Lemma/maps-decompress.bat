cd Game
ren *.map *.map.gz
gzip -d *.map.gz
cd Challenge
ren *.map *.map.gz
gzip -d *.map.gz
cd ../../Content
ren *.map *.map.gz
gzip -d *.map.gz
cd ..