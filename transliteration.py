from translitua import translit, ALL_UKRAINIAN
import csv

def get_all_unique(text):
	return list(set((map(lambda t:translit(text, t), ALL_UKRAINIAN))))

def translit_all(input, output):  
	with open(output, "a", encoding='utf-8') as wf:
		wf.truncate(0)
		wf.write('id,original,translitaretions\r')
		with open(input, encoding='utf-8') as f:
			reader = csv.reader(f, delimiter=",")
			for i, line in enumerate(reader):
				if i > 1:
					wf.write('{},{},{}\r'.format(line[0], line[1],','.join(get_all_unique(line[1]))))
		

if __name__ == "__main__":
	import sys
	translit_all(sys.argv[1], sys.argv[2])