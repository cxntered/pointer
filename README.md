<div align="center">

# `pointer!`

point your osu!lazer files back to stable! <br />
hard links[^1]/converts beatmaps, collections, skins, and scores[^2]

</div>

## why?

i main lazer. this is for when i want to play stable for whatever reason. <br />
also an excuse for me to learn c# :trollface:

## resources

- [file storage in osu!(lazer)](https://osu.ppy.sh/wiki/en/Client/Release_stream/Lazer/File_storage)
- [legacy database file structure](https://github.com/ppy/osu/wiki/Legacy-database-file-structure)
- [.osr (file format)](https://osu.ppy.sh/wiki/en/Client/File_formats/osr_%28file_format%29)

## disclaimer

pointer! is in no way affiliated with ppy or osu!

[^1]: falls back to copying files if hard links are not supported
[^2]: scores will use lazer scoring instead of scorev2, leading to small score differences. as well, scores using lazer only mods will not be converted
