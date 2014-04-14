'''
Created on Mar 12, 2013

@author: bli
'''

import sys, os, argparse
from os.path import exists, join, dirname, abspath
ScriptDir = abspath(dirname(__file__))
g_PosixLinebreak = '\n'

class WwiseIDConverter(object):
	'''Convert Wwise soundbank ID header from C++ to C#. Save it under the same folder of the input header.'''

	def __init__(self, inHeader, outputDirectory):
		self.inHeader = inHeader
		self.outHeader = join(outputDirectory, 'Wwise_IDs.cs')
	
	def Convert(self):
		lines = self._ImportFile(self.inHeader)

		# Extract ID part
		IDStartKey = 'namespace'
		startLine = self._FindKeyLine(lines, IDStartKey)
		IDEndKey = '#endif'
		endLine = self._FindKeyLine(lines, IDEndKey)
		lines = lines[startLine : endLine]
	
		# Use C# class for namespace
		CType = 'namespace'
		CSType = 'public class'
		self._ReplaceLineByLine(lines, CType, CSType)
	
	
		# Replace AK type with C# types
		CType = 'static const AkUniqueID'
		CSType = 'public static uint'
		self._ReplaceLineByLine(lines, CType, CSType)
	
		outDir = abspath(dirname(self.outHeader))
		if not os.path.exists(outDir):
			os.makedirs(outDir)
		self._ExportFile(self.outHeader, lines)
	
	def _ImportFile(self, inputFile):
		rawLines = []
		with open(inputFile) as f:
			rawLines = f.readlines()
			f.close()
		
		return rawLines
	
	def _ExportFile(self, outputFile, outputLines):
		# append line separators if none
		for ll in range(len(outputLines)):
			hasNoLinebreak = outputLines[ll].find(os.linesep) == -1 and outputLines[ll].find(g_PosixLinebreak) == -1
			if hasNoLinebreak:
				outputLines[ll] += g_PosixLinebreak
			
		with open(outputFile, 'w') as f:
			f.writelines(outputLines)
			f.close()
	
	
	def _FindKeyLine(self, lines, key):
		keyLineNumber = 0
		for ll in range(len(lines)):
			foundKey = lines[ll].find(key) != -1
			if foundKey:
				keyLineNumber = ll
				break
		return keyLineNumber
	
	def _ReplaceLineByLine(self, lines, inPattern, outPattern):
		for ll in range(len(lines)):
			namespaceStartCol = lines[ll].find(inPattern)
			foundNamespace = namespaceStartCol != -1
			if foundNamespace:
				lines[ll] = lines[ll].replace(inPattern, outPattern)
		
if __name__ == '__main__':
	parser = argparse.ArgumentParser(description='Convert Wwise SoundBank ID C++ header into C# for Unity.')
	parser.add_argument('WwiseIDHeader', action='store', default='UndefinedHeader', help='Full path to Wwise SoundBank ID C++ header, e.g., Wwise_IDs.h')
	parser.add_argument('OutputDirectory', action='store', default=None, help='Output directory (defaults to the same folder)')
	
	args = parser.parse_args()
	inHeader = args.WwiseIDHeader
	if not exists(inHeader):
		raise RuntimeError('Input header file does not exist: {}'.format(inHeader))
	if args.OutputDirectory is None:
		args.OutputDirectory = os.path.dirname(inHeader)
	
	converter = WwiseIDConverter(inHeader, args.OutputDirectory)
	converter.Convert()

