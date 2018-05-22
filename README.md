MidiAnimImporter
================

**MidiAnimImporter** is a custom importer that imports a .mid file (SMF;
Standard MIDI File) into an animation clip.

![screenshot](https://i.imgur.com/fJtYbnVl.png)

MidiAnimImporter generates three types of animation curves.

- **Beat/Bar Clock** - Indicates the timings of the beats (quarter notes) and
  the bars.
- **Note Curves** - Indicates the timings of key on/off of each note.
- **CC Curves** - Represents the animation of each CC (control change) value.

[A small example video](http://radiumsoftware.tumblr.com/post/165638886043).

Installation
------------

Download and import one of the unitypackage files in the [Releases page].

[Releases page]: https://github.com/keijiro/MidiAnimImporter/releases

How to Use
----------

MidiAnimImporter imports files with the `.midianim` extension. So the files
should be renamed to end with `.midianim` before importing.

There are a few settings in the inspector.

![inspector](https://i.imgur.com/HDWZgX7.png)

- **Track Index** - The MIDI file track index that will be imported.
- **BPM** - The importer doesn't support the BPM meta information, so that the
  BPM has to be set manually.
- **Gate Easing** - When enabled, the note curves are to be smoothed by adding
  some transition time.
- **Attack/Release Time** - Transition time of beginning/ending of each note.

The curves are to be imported as an animation clip of the `MidiState` class.
It's just a placeholder for the imported animations; They have to be manually
copy-and-pasted to actual animations.

Copy-and-paste by hand? No way!
-------------------------------

I think so too. This workflow should be improved in the future versions. If you
have a good idea for improving it, [toss it to me].

[toss it to me]: https://github.com/keijiro/MidiAnimImporter/issues

License
-------

[MIT](LICENSE.txt)
