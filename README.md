[![CodeFactor][CodeFactor-Badge]][CodeFactor-Url]

# AkkoBot
#### A moderation Discord bot written in C# with DSharpPlus

[Try it out in your server!][AkkoInvite]

Akko is a Discord bot focused on moderation that aims to make management of your community easier.
Features include, but are not limited to:

- Moderation
	- Kick, ban, or mute users (temporarily or permanently)
	- Issue infractions to misbehaving users and automatically punish them when they hit a certain threshold.
	- Lock and unlock text channels with ease
	- Delete messages in bulk
- Gatekeeping
	- Create greet and farewell messages for when users join and leave your server
	- Anti-alt filter (keep alt accounts out of your server)
	- Anti-hoisting (prevent users from changing their nickname just to display themselves at the top of the user list)
- Filtering
	- Invite filter (automatically deletes any message that contains a server invite)
	- Word filter (automatically deletes any message that contains blacklisted words)
	- Content filter (create channels that only accept messages with pictures, URLs, attachments, invites, or stickers in any combination you want)
- Utilities
	- Create polls for your community to vote on
	- Create reminders for yourself or your community
	- Create tags for your community to use (predefined messages that are triggered by specific keywords)
	- Create voice roles (roles that are automatically assigned to users when they join a voice channel)
	- Commands to quickly interface with Discord (so you don't have to access Discord's UI to do stuff)

## Getting Started

By default Akko responds to commands prefixed with `!`, but it also has a few slash commands to help you find and setup the features you're looking for. Just type `/` and scroll through the list.

After you [invite Akko to your server][AkkoInvite], you should [setup the permissions][Role101] needed for her to do her job. For example, if you want her to be able to kick users, you should give her permission to `Kick Members` and so on.

And that's it. You're set.

## More
- Want to learn about all the features? Check the full [command list][CommandList].
- Want to host your own Akko instance? Check the [installation guide][InstallationGuide].
- Do you have more questions? Check the project's [wiki][GithubWiki].
- Couldn't find what you're looking for? Ask in our [support server][SupportServer].

## Buy me a Coffee
- [Ko-fi]
- Bitcoin: 174QugE9Dnpb2dRUuhFtDY3PreJKnZajVk
- Ethereum: 0xDbF8045Cf606b6fF64A2f5Aa8904d91e854dc721

## License
Copyright 2021 Kotz

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

> http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

[AkkoInvite]: https://discord.com/api/oauth2/authorize?client_id=893158413402505299&permissions=274877909056&scope=applications.commands%20bot
[CommandList]: ../../wiki/Command-List
[InstallationGuide]: ../../wiki/Installation-Guide
[GithubWiki]: ../../wiki
[SupportServer]: https://discord.gg/dETvNP5Hyh
[Role101]: https://support.discord.com/hc/en-us/articles/214836687-Role-Management-101
[Ko-fi]: https://ko-fi.com/kaoticz
[CodeFactor-Url]: https://www.codefactor.io/repository/github/kaoticz/akkobot/overview/main
[CodeFactor-Badge]: https://www.codefactor.io/repository/github/kaoticz/akkobot/badge/main
