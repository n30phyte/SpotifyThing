using System;
using System.Collections.Generic;
using System.IO;

using System.Net;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpotifyThing
{
    class Program
    {
        static void Main()
        {
            Console.Write("Please input your username/ID: ");
            var uname = Console.ReadLine();
            var spotify = new SpotifyHandler(uname);
            var playlists = spotify.GetPlaylists();

            while (true)
            {

                for (var i = 0; i < playlists.Count; i++)
                {
                    Console.WriteLine($"{i} {playlists[i].Name}");
                }

                Console.Write("Which playlist would you like to export: ");
                // ReSharper disable once AssignNullToNotNullAttribute
                var response = int.Parse(Console.ReadLine());
                Console.WriteLine($"{playlists[response].Tracks.Total} tracks to process.");

                FileHandler.SaveToCSV(spotify.GetPlaylistTracks(playlists[response]));

            }

            // ReSharper disable once FunctionNeverReturns
        }

        class SpotifyHandler
        {
            SpotifyWebAPI _spotify;
            private string _apiKey = "6bf5a211811d4dcfa5179a8084de0637";
            private string _username;

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

        class FileHandler
        {

            public static void SaveToCSV(List<PlaylistTrack> Playlist)
            {
                var sw = new StreamWriter("output.csv");
                Console.WriteLine("Making new file output.csv");
                sw.WriteLine("sep=|");
                sw.WriteLine("Title|Artist|Album");

                foreach (var artist in Playlist[0].Track.Artists)
                { Console.WriteLine(artist.Name); }



                for (var i = 0; i < Playlist.Count; i++)
                {
                    ProgressBar(i, Playlist.Count);
                    var currentTrack = Playlist[i].Track;

                    var ArtistList = new List<string>();

                    foreach (var artist in currentTrack.Artists)
                    {
                        ArtistList.Add(artist.Name);
                    }

                    sw.WriteLine($"{currentTrack.Name}|{string.Join(",",ArtistList)}|{currentTrack.Album.Name}");
                }


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
                float onechunk = 30.0f / tot;

                //draw filled part
                int position = 1;
                for (int i = 0; i < onechunk * progress; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                //draw unfilled part
                for (int i = position; i <= 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                //draw totals
                Console.CursorLeft = 35;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(progress.ToString() + " of " + tot.ToString() +
                              "    "); //blanks at the end remove any excess
            }

        }
    }
}