import os
from typing import List

import tekore
import questionary
from dotenv import load_dotenv
import pyexcel


class Spotify:
    CONFIG_NAME = "tekore.cfg"

    SCOPES = [
        tekore.scope.user_library_read,
        tekore.scope.playlist_read_private,
        # tekore.scope.playlist_modify_private,
    ]

    CLIENT_ID = os.getenv("SPOTIFY_CLIENT_ID")
    CLIENT_SECRET = os.getenv("SPOTIFY_CLIENT_SECRET")
    REDIRECT_URI = os.getenv("SPOTIFY_REDIRECT_URI")

    spotify_client: tekore.Spotify = None

    def __init__(self):
        print("Authenticating with Spotify")
        token = self.authenticate()
        print("Authenticated")

        self.spotify_client = tekore.Spotify(token)

    def authenticate(self):
        credentials = (self.CLIENT_ID, self.CLIENT_SECRET, self.REDIRECT_URI)

        if not os.path.isfile(self.CONFIG_NAME):
            token = tekore.prompt_for_user_token(*credentials, self.SCOPES)

            tekore.config_to_file(
                self.CONFIG_NAME, credentials + (token.refresh_token,)
            )

            return token
        else:
            config = tekore.config_from_file(self.CONFIG_NAME, return_refresh=True)
            return tekore.refresh_user_token(config[0], config[1], config[3])

    def get_user_library(self):
        tracks: List[tekore.model.SavedTrack] = []

        start_offset_idx = 0
        while True:
            received_tracks = self.spotify_client.saved_tracks(
                limit=50, offset=start_offset_idx * 50
            ).items

            start_offset_idx += 1

            tracks.extend(received_tracks)

            if len(received_tracks) < 50:
                break

        return [i.track for i in tracks]

    def get_user_playlists(self) -> List[tekore.model.FullPlaylist]:
        playlists: List[tekore.model.SimplePlaylist] = []

        start_offset_idx = 0
        while True:
            received_playlists = self.spotify_client.playlists(
                self.spotify_client.current_user().id,
                limit=50,
                offset=start_offset_idx * 50,
            ).items

            start_offset_idx += 1

            playlists.extend(received_playlists)

            if len(received_playlists) < 50:
                break

        return [self.spotify_client.playlist(i.id) for i in playlists]


class App:
    def __init__(self):
        self.spotify = Spotify()

    def generate_fitness_playlist(self):
        pass

    def reorder_playlist_bpm(self):
        pass

    def playlist_download(self):
        playlists = self.spotify.get_user_playlists()

        choices = [questionary.Choice("Saved Music", value=0)]
        choices.extend(
            [
                questionary.Choice(playlists[i].name, value=i + 1)
                for i in range(len(playlists))
            ]
        )

        answers = questionary.form(
            filename=questionary.text("What should the file be named?"),
            playlist_idx=questionary.select(
                "Which playlist would you like to download?", choices=choices
            ),
        ).ask()

        playlist_idx = answers["playlist_idx"]

        tracklist: List[tekore.model.FullTrack]

        if playlist_idx == 0:
            # Saved music
            tracklist = self.spotify.get_user_library()
        else:
            # Other playlist
            paging_tracks = playlists[playlist_idx - 1].tracks
            tracklist = [
                track.track for track in paging_tracks.items if not track.is_local
            ]

        processed_tracks = [
            {
                "Title": track.name,
                "Album Artist": ", ".join(
                    [artist.name for artist in track.album.artists]
                ),
                "Album": track.album.name,
            }
            for track in tracklist
        ]

        pyexcel.save_as(
            records=processed_tracks, dest_file_name=answers["filename"] + ".xlsx"
        )

    def run(self):
        response = questionary.select(
            "What would you like to do?",
            choices=[
                questionary.Choice(
                    "Download playlist as CSV", value=self.playlist_download
                ),
                questionary.Choice(
                    "Reorder existing playlist based on BPM",
                    value=self.reorder_playlist_bpm,
                ),
                questionary.Choice(
                    "Generate Fitness Playlist", value=self.generate_fitness_playlist
                ),
            ],
        ).ask()

        response()


load_dotenv()

if __name__ == "__main__":
    App().run()
