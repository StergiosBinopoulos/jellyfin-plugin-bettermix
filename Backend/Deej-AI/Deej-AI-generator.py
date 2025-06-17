import os

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

import numpy as np
import pickle
import argparse

default_lookback = 3  # number of previous tracks to take into account
default_noise = 0  # amount of randomness to throw in the mix
default_playlist_size = 10


def most_similar(positive=[], negative=[], topn=5, noise=0):
    if isinstance(positive, str):
        positive = [positive]  # broadcast to list
    if isinstance(negative, str):
        negative = [negative]  # broadcast to list
    mp3_vec_i = np.sum([mp3tovec[i] for i in positive] + [-mp3tovec[i] for i in negative], axis=0)
    mp3_vec_i += np.random.normal(0, noise * np.linalg.norm(mp3_vec_i), len(mp3_vec_i))
    similar = []
    for track_j in mp3tovec:
        if track_j in positive or track_j in negative:
            continue
        mp3_vec_j = mp3tovec[track_j]
        cos_proximity = np.dot(mp3_vec_i, mp3_vec_j) / (np.linalg.norm(mp3_vec_i) * np.linalg.norm(mp3_vec_j))
        similar.append((track_j, cos_proximity))
    return sorted(similar, key=lambda x: -x[1])[:topn]

def most_similar_by_vec(positive=[], negative=[], topn=5, noise=0):
    if isinstance(positive, str):
        positive = [positive]  # broadcast to list
    if isinstance(negative, str):
        negative = [negative]  # broadcast to list
    mp3_vec_i = np.sum([i for i in positive] + [-i for i in negative], axis=0)
    mp3_vec_i += np.random.normal(0, noise * np.linalg.norm(mp3_vec_i), len(mp3_vec_i))
    similar = []
    for track_j in mp3tovec:
        mp3_vec_j = mp3tovec[track_j]
        cos_proximity = np.dot(mp3_vec_i, mp3_vec_j) / (np.linalg.norm(mp3_vec_i) * np.linalg.norm(mp3_vec_j))
        similar.append((track_j, cos_proximity))
    return sorted(similar, key=lambda x: -x[1])[:topn]

def make_playlist(seed_tracks, size=10, lookback=3, noise=0):
    max_tries = 10
    playlist = seed_tracks
    while len(playlist) < size:
        similar = most_similar(positive=playlist[-lookback:], topn=max_tries, noise=noise)
        candidates = [candidate[0] for candidate in similar if candidate[0] != playlist[-1]]
        for candidate in candidates:
            if not candidate in playlist:
                break
        playlist.append(candidate)
    return playlist

if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('pickles', type=str, help='Directory of pickled TrackToVecs')
    parser.add_argument('mp3tovec', type=str, help='Filename (without extension) of pickled MP3ToVecs')
    parser.add_argument('--model', type=str, help='Load spectrogram to Track2Vec model (default: "speccy_model")')
    parser.add_argument('--batchsize', type=int, help='Number of MP3s to process in each batch (default: 100)')
    parser.add_argument('--epsilon', type=float, help='Epsilon distance (default: 0.001)')
    parser.add_argument('--inputsong', type=str,
                        help="Requires --playlist option\nSelects a song to start the playlist with.")
    parser.add_argument("--nsongs", type=int, help="Requires --playlist option\nNumber of songs in the playlist")
    parser.add_argument("--noise", type=float,
                        help="Requires --playlist option\nAmount of noise in the playlist (default 0)")
    parser.add_argument("--lookback", type=int,
                        help="Requires --playlist option\nAmount of lookback in the playlist (default 3)")

    args = parser.parse_args()
    dump_directory = args.pickles
    mp3tovec_file = args.mp3tovec
    model_file = args.model
    batch_size = args.batchsize
    epsilon_distance = args.epsilon
    input_song = args.inputsong
    n_songs = args.nsongs
    noise = args.noise
    lookback = args.lookback

    if model_file == None:
        model_file = 'speccy_model'
    if batch_size == None:
        batch_size = 100
    if epsilon_distance == None:
        epsilon_distance = 0.001  # should be small, but not too small
    mp3tovec = pickle.load(open(dump_directory + '/mp3tovecs/' + mp3tovec_file + '.p', 'rb'))

    if input_song != None:
        if n_songs == None:
            n_songs = default_playlist_size
        if noise == None:
            noise = default_noise
        if lookback == None:
            lookback = default_lookback

        tracks = make_playlist([input_song], size=n_songs + 1, noise=noise, lookback=lookback)
        for item in tracks:
            print(item)