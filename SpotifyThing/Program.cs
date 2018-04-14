using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyThing
{
    internal class Program
    {
        private static void Main()
        {
            Console.Write("Please input your username/ID: ");
            var uname = Console.ReadLine();
            var spotify = new SpotifyHandler(uname);
            var playlists = spotify.GetPlaylists();

            while (true)
            {
                for (var i = 0; i < playlists.Count; i++) Console.WriteLine($"{i + 1} {playlists[i].Name}");

                Console.Write("Which playlist would you like to export (0 to exit): ");

                var response = int.Parse(Console.ReadLine());
                if (response == 0) Environment.Exit(0);

                var selectedPlaylist = playlists[response - 1];

                Console.WriteLine($"{selectedPlaylist.Tracks.Total} tracks to process.");
                FileHandler.SaveToCSV(spotify.GetPlaylistTracks(selectedPlaylist), selectedPlaylist.Name);
            }
        }

        private class SpotifyHandler
        {
            private readonly string _apiKey = "6bf5a211811d4dcfa5179a8084de0637";
            private readonly SpotifyWebAPI _spotify;
            private readonly string _username;

            public SpotifyHandler(string username)
            {
                var webApiFactory = new WebAPIFactory(
                    "http://localhost",
                    8000,
                    _apiKey,
                    Scope.UserReadPrivate,
                    TimeSpan.FromSeconds(20)
                );
                try
                {
                    _spotify = Task.Run(() => webApiFactory.GetWebApi()).Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                _username = username;
            }

            public List<SimplePlaylist> GetPlaylists()
            {
                return _spotify.GetUserPlaylists(_username).Items;
            }

            public List<PlaylistTrack> GetPlaylistTracks(SimplePlaylist selectedPlaylist)
            {
                var outputList = new List<PlaylistTrack>(selectedPlaylist.Tracks.Total);
                for (var j = 0; j < selectedPlaylist.Tracks.Total; j += 100)
                {
                    var playlistContent = _spotify.GetPlaylistTracks(_username, selectedPlaylist.Id, offset: j);
                    outputList.AddRange(playlistContent.Items);
                }

                return outputList;
            }
        }

        private class FileHandler
        {
            public static void SaveToCSV(List<PlaylistTrack> Playlist, string PlaylistName)
            {
                var sw = new StreamWriter($"{PlaylistName}.csv");
                Console.WriteLine($"Making new file {PlaylistName}.csv");
                sw.WriteLine("sep=|");
                sw.WriteLine("Title|Artist|Album");

                foreach (var artist in Playlist[0].Track.Artists) Console.WriteLine(artist.Name);


                for (var i = 0; i < Playlist.Count; i++)
                {
                    ProgressBar(i + 1, Playlist.Count);
                    var currentTrack = Playlist[i].Track;

                    var artistList = new List<string>();

                    foreach (var artist in currentTrack.Artists) artistList.Add(artist.Name);

                    sw.WriteLine($"{currentTrack.Name}|{string.Join(",", artistList)}|{currentTrack.Album.Name}");
                }

                Console.WriteLine();

                sw.Close();
            }

            private static void ProgressBar(int progress, int tot)
            {
                //draw empty progress bar
                Console.CursorLeft = 0;
                Console.Write("["); //start
                Console.CursorLeft = 32;
                Console.Write("]"); //end
                Console.CursorLeft = 1;
                var onechunk = 30.0f / tot;

                //draw filled part
                var position = 1;
                for (var i = 0; i < onechunk * progress; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                //draw unfilled part
                for (var i = position; i <= 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                //draw totals
                Console.CursorLeft = 35;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(progress + " of " + tot +
                              "    "); //blanks at the end remove any excess
            }
        }
    }
}