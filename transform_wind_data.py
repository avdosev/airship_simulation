import json
import numpy as np 


def UVlalo(U_data, V_data):
    lo_start, lo_end = 0, 360
    la_start, la_end = 90, -91
    lo = lo_start
    la = la_start
    for U, V in zip(U_data, V_data):
        yield U, V, la, lo
        lo += 1
        if lo == lo_end:
            lo = lo_start
            la -= 1
            if la == la_end:
                pass


def transform_weather_data(input_filename, output_filename, sep=','):
    with open(input_filename, 'r') as f:
        data = json.load(f)
        U_data = data[0]['data']
        V_data = data[1]['data']


        with open(output_filename, 'w') as wf:
            print('U', 'V', 'la', 'lo', file=wf, sep=sep)
            for U, V, la, lo in UVlalo(U_data, V_data):
                print(U, V, la, lo, file=wf, sep=sep)



def main():
    transform_weather_data(
        './data/current-wind-surface-level-gfs-1.0.json',
        './data/current-wind.csv')


if __name__ == "__main__":
    main()