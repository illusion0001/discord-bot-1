import discord
import asyncio
import requests
import re
import urllib.parse
import json
from bs4 import BeautifulSoup
from discord.ext.commands import Bot
import sys

channelid = "291679908067803136"
rpcs3Bot = Bot(command_prefix="!")
pattern = '[A-z]{4}\\d{5}'

@rpcs3Bot.event
async def on_message(message):
	if message.author.name == "RPCS3 Bot":
		return
	try:
		if message.content[0] == "!":
			return await rpcs3Bot.process_commands(message)
	except IndexError as ie:
		print(message.content)
		return
	codelist = []
	for matcher in re.finditer(pattern, message.content):
		code = matcher.group(0)
		if code not in codelist:
			codelist.append(code)
			print(code)
	for code in codelist:
		info = await getCode(code)
		if not info == "None":
			await rpcs3Bot.send_message(message.channel, info)

@rpcs3Bot.command()
async def credits(*args):
	"""Author Credit"""
	return await rpcs3Bot.say("```\nMade by Roberto Anic Banic aka nicba1010!\n```")

@rpcs3Bot.command(pass_context=True)
async def c(ctx, *args):
	"""Searches the compatibility database, USE: !c searchterm """
	await compatSearch(ctx, *args)

@rpcs3Bot.command(pass_context=True)
async def compat(ctx, *args):
	"""Searches the compatibility database, USE: !compat searchterm"""
	await compatSearch(ctx, *args)

@rpcs3Bot.command(pass_context=True)
async def latest(ctx, *args):
	"""Get the latest RPCS3 build link"""
	appveyor_url = BeautifulSoup(requests.get("https://rpcs3.net/download").content, "lxml").find("div", {"class": "div-download-left"}).parent['href']
	return await rpcs3Bot.send_message(ctx.message.author, appveyor_url)

async def compatSearch(ctx, *args):
	print(ctx.message.channel)
	if ctx.message.channel.id != "319224795785068545":
		return
	escapedSearch = ""
	unescapedSearch = ""
	for arg in args:
		escapedSearch += "+{}".format(urllib.parse.quote(arg))
	for arg in args:
		unescapedSearch += " {}".format(arg)
	escapedSearch = escapedSearch[1:]
	unescapedSearch = unescapedSearch[1:]
	if len(unescapedSearch) < 3:
		return await rpcs3Bot.send_message(discord.Object(id=channelid), "{} please use 3 or more characters!".format(ctx.message.author.mention))
	url = "https://rpcs3.net/compatibility?g={}&r=1&api=v1".format(escapedSearch)
	jsonn = requests.get(url).text
	data = json.loads(jsonn)
	if data["return_code"] == -3:
		return await rpcs3Bot.send_message(discord.Object(id=channelid), "{}, Illegal search".format(ctx.message.author.mention))
	if data["return_code"] == -2:
		return await rpcs3Bot.send_message(discord.Object(id=channelid), "Please be patient API is in maintenance mode!")
	if data["return_code"] == -1:
		return await rpcs3Bot.send_message(discord.Object(id=channelid), "API Internal Error")
	#if data["return_code"] == 2:
	#	await rpcs3Bot.send_message(discord.Object(id=channelid), ", no result found, displaying alternatives for {}!".format(ctx.message.author.mention, unescapedSearch))
	if data["return_code"] == 1:
		return await rpcs3Bot.send_message(discord.Object(id=channelid), "{} searched for {} no result found!".format(ctx.message.author.mention, unescapedSearch))	
	await rpcs3Bot.send_message(discord.Object(id=channelid), "{} searched for: {}{}".format(ctx.message.author.mention, unescapedSearch, " " if data["return_code"] == 0 else "\n\tNo results found! Displaying alternatives!"))
	results = "```"
	result_arr = data["results"]
	
	for id, info in result_arr.items():
		title = info["title"]
		if len(title) > 40:
			title = "{}...".format(title[:37])
		results += "\nID:{:9s} Title:{:40s} Build:{:8s} Status:{:8s} Updated:{:10s}".format(id, title, "Unknown" if info["commit"] == 0 else str(info["commit"]), info["status"], info["date"])
	results += "\n```"
	await rpcs3Bot.send_message(discord.Object(id=channelid), results)
	return await rpcs3Bot.send_message(discord.Object(id=channelid), "Retrieved from: {}".format(url.replace("&api=v1", "")))

async def getCode(code):
	url = "https://rpcs3.net/compatibility?g={}&r=1&api=v1".format(code)
	jsonn = requests.get(url).text
	data = json.loads(jsonn)
	if data["return_code"] == -2:
		return "None"
		#return await rpcs3Bot.send_message(discord.Object(id=channelid), "Please be patient API is in maintenance mode!")
	if data["return_code"] == 0:
		result_arr = data["results"]
		for id, info in result_arr.items():
			title = info["title"]
			if len(title) > 40:
				title = "{}...".format(title[:37])
			result = "```\nID:{:9s} Title:{:40s} Build:{:8s} Status:{:8s} Updated:{:10s}\n```".format(id,  title, "Unknown" if info["commit"] == 0 else str(info["commit"]), info["status"], info["date"])
			return result
	return "None"

print(sys.argv[1])
rpcs3Bot.run(sys.argv[1])
