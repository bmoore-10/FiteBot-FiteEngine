# FiteBot (+ FiteEngine)

Fitebot is a Discord bot written in [Discord.Net](https://github.com/discord-net/Discord.Net) to interface with FiteEngine, which is a library written to create and manage a database of players, games, and other data involved in locally keeping track of a community's internal rankings in various fighting games. Rating is determined via an implementation of the [Glicko-2](http://www.glicko.net/glicko/glicko2.pdf) algorithm.

## Usage

Build the project or extract the latest release into a folder

Run FiteBot.exe

Once it crashes, navigate to the freshly created Data folder and add your bot's client key to the config.txt. Do not add any spaces. Example syntax with gibberish key:
   
    ClientKey:aposiudhf_gashodfash_odfashf

Run FiteBot.exe once again. Now, the bot should be online and ready for use.

## Info for server owners

#### FiteBot looks for certain hardcoded values to determine various user permissions:

Upon registration, FiteBot attempts to give the newly registered player the first role it sees in the server that is [Magenta](https://i.imgur.com/uBzcjK3.png) colored. To check that a player is allowed to run member commands, FiteBot checks that the user has the permission to read the message history of the channel they're messaging in. **For proper operation, please make sure that your registered user role is the only role that is magenta colored and that it has the Read History Of Channel permission in any channel where FiteBot commands are allowed to be run**.

###### (The function to check for a magenta colored role is written fairly naively and simply returns the first magenta colored role it sees. Technically, this means that you're free to have multiple magenta colored roles in your server...you just probably shouldn't.)

For moderator roles, FiteBot checks that the person running a command has the ability to manage messages in the server. **For proper operation, please make sure that your moderator role has the manage messages permission**. Furthermore, please note that any other roles that have the permission to manage messages will be able to run FiteBot's moderator-only commands. Please take this into consideration when managing your server's roles.
