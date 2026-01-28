# MVNFOEditor [Beta]
# NOTE - Still in Beta!
> This project has been sitting around "completed" for quite some time and i've been chipping away at bugs when I've had a chance - it's close enough now for it to be publically available but it has some bugs and quirks. The key functionality is solid, though, and I've found it super useful in bolstering my MTV Channel on my local [ErsatzTV](https://github.com/ErsatzTV/ErsatzTV) instance. Just keep in mind the UI navigation isn't perfect but I am working on it!

# What is it?

MVNFOEditor (Working Title) is a media manager centered around Music Videos with built-in support for browding and downloading videos via the supported services, generating an NFO Metadata file alongside the file for external use.
YouTube videos can also be added to the manager via a direct link for download.
Videos can be easily edited at any time, along with associated items like Artists/Albums with the changes being reflected in the relevent video's NFO files.

# Supported Services
- Youtube Music
- Apple Music

# Download
Grab the latest version from the [releases](https://github.com/Hendo16/MVNFOEditor/releases) page and extract the latest beta build into a folder you have write-permissions in.

# Downloading Support
By default, anyone can download videos through YouTube Music without an account via the [YtMusicNet](https://github.com/Hendo16/YtMusicNet) library - however, to download through Apple Music you will need a valid Apple Music account.

Please see the [wiki](https://github.com/Hendo16/MVNFOEditor/wiki) for a guide on setting up Authentication as well as a walk-through on how to use the app 

# Credits
- [SukiUI](https://github.com/kikipoulet/SukiUI) for providing an amazing Avalonia desktop UI library as well as including a fantastic demo app that provided some of the key infrastructure for this. As a Back-End oriented developer, this was a godsend!
- [Manzana](https://github.com/dropcreations/Manzana-Apple-Music-Downloader) for providing a lot of the leg work to get Apple Music integration working
- [WVCore](https://github.com/nilaoda/WVCore) for providing a nice .NET library to handle Widevine decryption
- [ErsatzTV](https://github.com/ErsatzTV/ErsatzTV) for building an amazing application that inspired me to create a utility to better manage my own Music Video channels
- And to Apple for keeping the [iTunes Search API](https://performance-partners.apple.com/search-api) live and unauthenticated!
