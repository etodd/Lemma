#!/usr/bin/python

import os
import jinja2

# Compiles jinja2 templates

INPUT_DIR = '.'
OUTPUT_DIR = 'output'
EXTENSION = '.html'

def IGNORE(filename):
	return filename[0] == '_'

# Delete old files
for f in (f for f in os.listdir(OUTPUT_DIR) if os.path.isfile(f) and os.path.splitext(f)[1] == EXTENSION):
	os.unlink(os.path.join(OUTPUT_DIR, f))

# Compile
env = jinja2.Environment(loader = jinja2.FileSystemLoader(INPUT_DIR))

for f in (f for f in os.listdir(INPUT_DIR) if os.path.isfile(f) and not IGNORE(f) and os.path.splitext(f)[1] == EXTENSION):
	with open(os.path.join(OUTPUT_DIR, os.path.splitext(f)[0]), 'w') as newFile:
		newFile.write(env.get_template(f).render())