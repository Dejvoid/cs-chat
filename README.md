# cs-chat –⁠⁠⁠ C# Course Assignment (2023)

Author: David Hřivna

Keywords: C#, Windows Forms, Chat, Sockets


## Introduction

This project is my final assignment for the C# course at MFF UK. Idea was to create a C# socket chat server and client. Server is pure CLI. Client is available as CLI and Windows Forms app.

## Available chat commands
- `/pm <username> <message>` - sends the private message to a user
- `/gm <groupname> <message>` - sends the message to the group
- `/group join <groupname>` - join the public group
- `/group create <groupname> <grouptype>` - create new private or public group
- `/group leave <groupname>` - leave the group
- `/group invite <groupname <username>` - invite the user to the group
- `/group promote <groupname <username>` - promote the user to the group admin
- `/group kick <groupname> <username>` - kick user from the group
- `/help` - prints the help

## Technical information

Project is developed in .NET 6.0. 
Developed in Visual Studio 2022 Community.

### Classes and Interfaces
- `IChatServer`, (`IDisposable`)
  -  `Server`
- (`IDisposable`)
  - `Client`
- `MessageWrapper`
- `ClientWrapper`
