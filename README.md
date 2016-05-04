# WebComicDownloader
A little Console Utility to download blog based webcomics

It's only in beta stage, since there are some little issues left.

##Master Branch Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/lsuvijvevir2rk88/branch/master?svg=true)](https://ci.appveyor.com/project/sidewinder94/webcomicdownloader/branch/master)

Download Latest Successfull build (please keep in mind that these are experimental builds) : [https://ci.appveyor.com/api/projects/sidewinder94/webcomicdownloader/artifacts/WebComicToEbook/bin/Release.zip](https://ci.appveyor.com/api/projects/sidewinder94/webcomicdownloader/artifacts/WebComicToEbook/bin/Release.zip)

##Known Issues
- When using the RegExp scraper, the encoding might get screwed.
- When using the XPath scraper, the inner xml of the selected elements will not get saved (thus losing the style).
- All of the exceptions are not properly handled yet, the program migth thus end unexpectedly
- ~~Display screwed when scapring more than one webcomic at once~~
- ~~unexpectedly hig memory consumption~~


##Usage
As of now there are **two** : 
- -i or --input : allows to specify a custom configuration file, if it does not exists, it will be created with an empty entry at this path
- -o or --overwrite : if this flag is set, instead of renaming new ebooks with the name of existing files, will delete the old files before saving the new ones.


The program will try to read a config.json file in the running directory.
If none are found, an empty one will be created
An example of such a configuration file would be :
```json
[
    {
        "Parser": "RegExp",
        "BaseAddress": "http://beyondtheimpossible.org/comic/1-before-the-beginning-2/",
        "NextButtonSelector": "(?:href=\"(\\S+)\")? class=\"comic-nav-base comic-nav-next\">",
        "ChapterTitleSelector": "class=\"post-title\">([^<]*)<",
        "ChapterContentSelector": "<div class=\"entry\">((?:.|\n)*)<div class=\"post-extras\">",
        "Author": "Ffurla",
        "Date": "2016-03-18T13:24:36.2855417+01:00",
        "Title": "Beyond the Impossible",
        "Description": null
    },
    {
        "Parser": "XPath",
        "BaseAddress": "http://www.wuxiaworld.com/tdg-index/tdg-chapter-1/",
        "NextButtonSelector": "//@href[text() = 'Next Chapter']",
        "ChapterTitleSelector": "//article//strong",
        "ChapterContentSelector": "//*[@class='entry-content']/p[position() > 2]",
        "Author": "Unknown",
        "Date": "2016-03-18T13:24:36.2855417+01:00",
        "Title": "Tales of Demons And Gods",
        "Description": "Translated by Thyaeria"
    },
    {
        "Parser": "XPath",
        "BaseAddress": "http://beyondtheimpossible.org/comic/1-before-the-beginning-2/",
        "NextButtonSelector": "//@href[@class='comic-nav-base comic-nav-next']",
        "ChapterTitleSelector": "//*[@class='post-title']",
        "ChapterContentSelector": "//*[@class='entry']",
        "Author": "Ffurla",
        "Date": "2016-03-18T13:24:36.2855417+01:00",
        "Title": "Beyond the Impossible",
        "Description": null
    }
]
```

The two first objects matches the same WebComic but are using different parsers.
The `Parser` property can only be `RegExp` ou `XPath` and it is not case sensitive.
