# PizzaLight
PizzaLight is your friendly neighborhood Pizza event organizer. Inviting your friends to enjoy tasty pizza together via slack.

## About
 - Runs in the channel specified in config. 
 - Periodically invites random members to a pizza date and handles invitations and reminders.

## How To Run
Clone from git and build docker image
```
docker build -t pizzabot:latest https://github.com/jarleli/PizzaLight.git
```
Run the docker image and set up a folder for persistence and a port for forwarding
```
docker run --rm -it -v /data/pizzalight/:/app/pizzalight/data/ -p 5000:5000 pizzabot
```
You must make a data folder at /data/pizzalight/ to hold your config with apitoken and a file called state.json to start the application. This is linked into the docker container in the above command.