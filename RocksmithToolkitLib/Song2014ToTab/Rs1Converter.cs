﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using RocksmithToolkitLib.DLCPackage.Manifest.Tone;
using RocksmithToolkitLib.Extensions;
using RocksmithToolkitLib.Sng;
using RocksmithToolkitLib.SngToTab;
using RocksmithToolkitLib.Xml;
using SongLevel = RocksmithToolkitLib.Xml.SongLevel;

namespace RocksmithToolkitLib.Song2014ToTab
{
    public class Rs1Converter : IDisposable
    {
        #region Song XML file to Song Object
        /// <summary>
        /// Convert XML file to RS1 (Song)
        /// </summary>
        /// <param name="xmlFilePath"></param>
        /// <returns>Song</returns>
        public Song XmlToSong(string xmlFilePath)
        {
            Song song = Song.LoadFromFile(xmlFilePath);
            return song;
        }
        #endregion

        #region Song Object to Song Xml file

        /// <summary>
        /// Convert RS1 Song Object to RS1 Song Xml file
        /// </summary>
        /// <param name="rs1Song"></param>
        /// <param name="outputDir"></param>
        /// <param name="overWrite"></param>
        /// <returns></returns>
        public string SongToXml(Song rs1Song, string outputDir)
        {
            // apply consistent file naming
            var title = rs1Song.Title;
            var arrangement = rs1Song.Arrangement;
            int posOfLastDash = rs1Song.Title.LastIndexOf(" - ");
            if (posOfLastDash != -1)
            {
                title = rs1Song.Title.Substring(0, posOfLastDash);
                arrangement = rs1Song.Title.Substring(posOfLastDash + 3);
            }

            var outputFile = String.Format("{0}_{1}", title, arrangement);
            outputFile = String.Format("{0}{1}", outputFile.GetValidName(false, true), "_Rs1.xml");
            var outputPath = Path.Combine(outputDir, outputFile);

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using (var stream = File.OpenWrite(outputPath))
            {
                rs1Song.Serialize(stream, false); ;
            }

            return outputPath;
        }
        #endregion

        #region Song Object to SngFile Object
        /// <summary>
        /// Converts RS1 Song Object to RS1 SngFile Object
        /// </summary>
        /// <param name="rs1Song"></param>
        /// <returns>SngFile</returns>
        public SngFile Song2SngFile(Song rs1Song, string outputDir)
        {
            var rs1SngPath = SongToSngFilePath(rs1Song, outputDir);
            SngFile sngFile = new SngFile(rs1SngPath);
            return sngFile;
        }
        #endregion

        #region Song to SngFilePath
        /// <summary>
        /// Converts RS1 Song Object to *.sng File
        /// </summary>
        /// <param name="rs1Song"></param>
        /// <returns>Path to binary *.sng file</returns>
        public string SongToSngFilePath(Song rs1Song, string outputDir)
        {
            string rs1XmlPath;
            using (var obj = new Rs1Converter())
                rs1XmlPath = obj.SongToXml(rs1Song, outputDir);

            ArrangementType arrangementType;
            if (rs1Song.Arrangement.ToLower() == "bass")
                arrangementType = ArrangementType.Bass;
            else
                arrangementType = ArrangementType.Guitar;

            var sngFilePath = Path.ChangeExtension(rs1XmlPath, ".sng");
            SngFileWriter.Write(rs1XmlPath, sngFilePath, arrangementType, new Platform(GamePlatform.Pc, GameVersion.None));

            if (File.Exists(rs1XmlPath)) File.Delete(rs1XmlPath);

            return sngFilePath;
        }
        #endregion

        #region SngFilePath to ASCII Tablature
        /// <summary>
        /// SngFilePath to ASCII Tablature
        /// </summary>
        /// <param name="inputFilePath"></param>
        /// <param name="outputDir"></param>
        /// <param name="allDif"></param>
        public void SngFilePathToAsciiTab(string inputFilePath, string outputDir, bool allDif)
        {
            using (var obj = new Sng2Tab())
                obj.Convert(inputFilePath, outputDir, allDif);
        }
        #endregion

        #region Song to Song2014
        /// <summary>
        /// Convert RS1 Song Object to RS2 Song2014 Object
        /// RS1 to RS2014 Mapping Method
        /// </summary>
        /// <param name="rsSong"></param>
        /// <param name="srcPath"></param>
        /// <returns>Song2014</returns>
        public Song2014 SongToSong2014(Song rsSong)
        {
            Song2014 rsSong2014 = new Song2014();

            // Song to Song2014 Mapping
            // metaheader elements 
            // TODO: get general info from RS1 song.manifest.json file
            rsSong2014.Version = "7";
            rsSong2014.Title = FilterTitle(rsSong);
            rsSong2014.Arrangement = rsSong.Arrangement;
            rsSong2014.Part = rsSong.Part;
            rsSong2014.Offset = rsSong.Offset;
            rsSong2014.CentOffset = "0";
            rsSong2014.SongLength = rsSong.SongLength;
            rsSong2014.LastConversionDateTime = DateTime.Now.ToString("MM-dd-yy HH:mm");
            rsSong2014.StartBeat = rsSong.Ebeats[0].Time;
            // if RS1 CDLC Song XML originates from EOF it may
            // already contain AverageTempo otherwise it gets calculated 
            rsSong2014.AverageTempo = rsSong.AverageTempo == 0 ? AverageBPM(rsSong) : rsSong.AverageTempo;

            // TODO: get tuning from RS1 tone.manifest.json file
            rsSong2014.Tuning = new TuningStrings { String0 = 0, String1 = 0, String2 = 0, String3 = 0, String4 = 0, String5 = 0 };

            rsSong2014.Capo = 0;
            // TODO: get song info from RS1 song.manifest.json file
            // force user to complete information or fills in when imported
            // rsSong2014.ArtistName = "Unknown Artist";
            // rsSong2014.AlbumName = "Unknown Album";
            rsSong2014.AlbumYear = DateTime.Now.ToString("yyyy");
            rsSong2014.CrowdSpeed = "1";

            // default arrangment properties
            rsSong2014.ArrangementProperties = new SongArrangementProperties2014
            {
                Represent = 1,
                StandardTuning = 1,
                NonStandardChords = 0,
                BarreChords = 0,
                PowerChords = 0,
                DropDPower = 0,
                OpenChords = 0,
                FingerPicking = 0,
                PickDirection = 0,
                DoubleStops = 0,
                PalmMutes = 0,
                Harmonics = 0,
                PinchHarmonics = 0,
                Hopo = 0,
                Tremolo = 0,
                Slides = 0,
                UnpitchedSlides = 0,
                Bends = 0,
                Tapping = 0,
                Vibrato = 0,
                FretHandMutes = 0,
                SlapPop = 0,
                TwoFingerPicking = 0,
                FifthsAndOctaves = 0,
                Syncopation = 0,
                BassPick = 0,
                Sustain = 0,
                BonusArr = 0,
                RouteMask = 1,
                PathLead = rsSong2014.Arrangement == "Lead" ? 1 : 0,
                PathRhythm = rsSong2014.Arrangement == "Rhythm" ? 1 : 0,
                PathBass = rsSong2014.Arrangement == "Bass" ? 1 : 0
            };

            // tone defaults used to produce RS2014 CDLC
            rsSong2014.ToneBase = "Default";
            rsSong2014.ToneA = "Default";
            rsSong2014.ToneB = "";
            rsSong2014.ToneC = "";
            rsSong2014.ToneD = "";

            // these elements have direct mappings
            rsSong2014.Phrases = rsSong.Phrases;
            // rsSong2014.LinkedDiffs = rsSong.LinkedDiffs;
            rsSong2014.LinkedDiffs = new SongLinkedDiff[0]; // prevents hanging
            // rsSong2014.PhraseProperties = rsSong.PhraseProperties;
            rsSong2014.PhraseProperties = new SongPhraseProperty[0]; // prevents hanging
            rsSong2014.FretHandMuteTemplates = rsSong.FretHandMuteTemplates;
            rsSong2014.Ebeats = rsSong.Ebeats;
            rsSong2014.Sections = rsSong.Sections;
            rsSong2014.Events = rsSong.Events;

            // these elements have no direct mapping
            rsSong2014 = ConvertTones(rsSong, rsSong2014);
            rsSong2014 = ConvertChordTemplates(rsSong, rsSong2014);
            rsSong2014 = ConvertNewLinkedDiff(rsSong, rsSong2014);
            rsSong2014 = ConvertLevels(rsSong, rsSong2014);
            rsSong2014 = ConvertPhraseIterations(rsSong, rsSong2014);

            return rsSong2014;
        }

        private string FilterTitle(Song rsSong)
        {
            string title = String.Empty;
            int index = rsSong.Title.IndexOf(" Combo");
            title = rsSong.Title.Substring(0, index);
            return title;
        }

        private float AverageBPM(Song rsSong)
        {
            // a rough approximation of BPM based on ebeats and time
            float beats = rsSong.Ebeats.Length;
            float endTimeMins = rsSong.Ebeats[rsSong.Ebeats.Length - 1].Time / 60;
            // float endTimeMins = rsSong.SongLength / 60;
            float avgBPM = (float)Math.Round(beats / endTimeMins, 1);

            return avgBPM;
        }

        private Song2014 ConvertTones(Song rsSong, Song2014 rsSong2014)
        {
            // no parallel element in RS1 so only initialiaze 
            rsSong2014.Tones = new SongTone2014[0];
            return rsSong2014;
        }
        private Song2014 ConvertChordTemplates(Song rsSong, Song2014 rsSong2014)
        {
            // add chordTemplates elements
            var chordTemplate = new List<SongChordTemplate2014>();
            foreach (var songChordTemplate in rsSong.ChordTemplates)
            {
                chordTemplate.Add(new SongChordTemplate2014
                {
                    ChordName = songChordTemplate.ChordName,
                    DisplayName = songChordTemplate.ChordName,
                    Finger0 = (sbyte)songChordTemplate.Finger0,
                    Finger1 = (sbyte)songChordTemplate.Finger1,
                    Finger2 = (sbyte)songChordTemplate.Finger2,
                    Finger3 = (sbyte)songChordTemplate.Finger3,
                    Finger4 = (sbyte)songChordTemplate.Finger4,
                    Finger5 = (sbyte)songChordTemplate.Finger5,
                    Fret0 = (sbyte)songChordTemplate.Fret0,
                    Fret1 = (sbyte)songChordTemplate.Fret1,
                    Fret2 = (sbyte)songChordTemplate.Fret2,
                    Fret3 = (sbyte)songChordTemplate.Fret3,
                    Fret4 = (sbyte)songChordTemplate.Fret4,
                    Fret5 = (sbyte)songChordTemplate.Fret5
                });
            }
            rsSong2014.ChordTemplates = chordTemplate.ToArray();
            return rsSong2014;
        }

        private Song2014 ConvertNewLinkedDiff(Song rsSong, Song2014 rsSong2014)
        {
            // no parallel element in RS1 so only initialiaze 
            rsSong2014.NewLinkedDiff = new SongNewLinkedDiff[0];
            return rsSong2014;
        }

        private Song2014 ConvertLevels(Song rsSong, Song2014 rsSong2014)
        {
            // add levels elements
            var levels = new List<SongLevel2014>();

            foreach (var songLevel in rsSong.Levels)
            {
                var anchors = new List<SongAnchor2014>();
                var notes = new List<SongNote2014>();
                var chords = new List<SongChord2014>();
                var handShapes = new List<SongHandShape>();

                for (int anchorIndex = 0; anchorIndex < songLevel.Anchors.Length; anchorIndex++)
                {
                    var anchor = songLevel.Anchors[anchorIndex];
                    anchors.Add(new SongAnchor2014 { Fret = anchor.Fret, Time = anchor.Time, Width = 4 });
                }

                for (int noteIndex = 0; noteIndex < songLevel.Notes.Length; noteIndex++)
                {
                    var songNote = songLevel.Notes[noteIndex];
                    notes.Add(GetNoteInfo(songNote));
                }

                for (int chordIndex = 0; chordIndex < songLevel.Chords.Length; chordIndex++)
                {
                    // RS1 does not contain chordNotes so need to make them from chordtemplate
                    List<SongNote2014> chordNotes = new List<SongNote2014>();
                    var zChord = songLevel.Chords[chordIndex];
                    var zChordId = zChord.ChordId;
                    var zChordTemplate = rsSong.ChordTemplates[zChordId];

                    if (zChordTemplate.Finger0 != -1) // finger > -1 is a string played                       
                        chordNotes.Add(DecodeChordTemplate(zChord, 0, zChordTemplate.Fret0));

                    if (zChordTemplate.Finger1 != -1)
                        chordNotes.Add(DecodeChordTemplate(zChord, 1, zChordTemplate.Fret1));

                    if (zChordTemplate.Finger2 != -1)
                        chordNotes.Add(DecodeChordTemplate(zChord, 2, zChordTemplate.Fret2));

                    if (zChordTemplate.Finger3 != -1)
                        chordNotes.Add(DecodeChordTemplate(zChord, 3, zChordTemplate.Fret3));

                    if (zChordTemplate.Finger4 != -1)
                        chordNotes.Add(DecodeChordTemplate(zChord, 4, zChordTemplate.Fret4));

                    if (zChordTemplate.Finger5 != -1)
                        chordNotes.Add(DecodeChordTemplate(zChord, 5, zChordTemplate.Fret5));

                    if (chordNotes.Any())
                        chords.Add(new SongChord2014 { ChordId = zChord.ChordId, ChordNotes = chordNotes.ToArray(), HighDensity = zChord.HighDensity, Ignore = zChord.Ignore, Strum = zChord.Strum, Time = zChord.Time });

                    // add chordNotes to songNotes for compatibility
                    notes.AddRange(chordNotes);
                }

                // get rid of duplicate notes if any
                var distinctNotes = notes.Distinct().ToList();

                for (int shapeIndex = 0; shapeIndex < songLevel.HandShapes.Length; shapeIndex++)
                {
                    var handshape = songLevel.HandShapes[shapeIndex];
                    handShapes.Add(new SongHandShape { ChordId = handshape.ChordId, EndTime = handshape.EndTime, StartTime = handshape.StartTime });
                }

                levels.Add(new SongLevel2014 { Anchors = anchors.ToArray(), Chords = chords.ToArray(), Difficulty = songLevel.Difficulty, HandShapes = handShapes.ToArray(), Notes = distinctNotes.ToArray() });
            }

            rsSong2014.Levels = levels.ToArray();

            return rsSong2014;
        }

        private Song2014 ConvertPhraseIterations(Song rsSong, Song2014 rsSong2014)
        {
            var phraseIterations = new List<SongPhraseIteration2014>();
            foreach (var songPhraseIteration in rsSong.PhraseIterations)
            {
                phraseIterations.Add(new SongPhraseIteration2014 { PhraseId = songPhraseIteration.PhraseId, Time = songPhraseIteration.Time });
            }
            rsSong2014.PhraseIterations = phraseIterations.ToArray();

            return rsSong2014;
        }

        private SongNote2014 GetNoteInfo(SongNote songNote)
        {
            SongNote2014 songNote2014 = new SongNote2014();
            songNote2014.Bend = (byte)songNote.Bend;
            songNote2014.Fret = (sbyte)songNote.Fret;
            songNote2014.HammerOn = (byte)songNote.HammerOn;
            songNote2014.Harmonic = (byte)songNote.Harmonic;
            songNote2014.Hopo = (byte)songNote.Hopo;
            songNote2014.Ignore = (byte)songNote.Ignore;
            songNote2014.PalmMute = (byte)songNote.PalmMute;
            songNote2014.Pluck = (sbyte)songNote.Pluck;
            songNote2014.PullOff = (byte)songNote.PullOff;
            songNote2014.Slap = (sbyte)songNote.Slap;
            songNote2014.SlideTo = (sbyte)songNote.SlideTo;
            songNote2014.String = (byte)songNote.String;
            songNote2014.Sustain = (float)songNote.Sustain;
            songNote2014.Time = (float)songNote.Time;
            songNote2014.Tremolo = (byte)songNote.Tremolo;
            // initialize elements not present in RS1
            songNote2014.LinkNext = 0;
            songNote2014.Accent = 0;
            songNote2014.LeftHand = -1;
            songNote2014.Mute = 0;
            songNote2014.HarmonicPinch = 0;
            songNote2014.PickDirection = 0;
            songNote2014.RightHand = -1;
            songNote2014.SlideUnpitchTo = -1;
            songNote2014.Tap = 0;
            songNote2014.Vibrato = 0;

            return songNote2014;
        }

        private SongNote2014 DecodeChordTemplate(SongChord songChord, int gString, int fret)
        {
            // RS2014
            //<chord time="83.366" linkNext="0" accent="0" chordId="19" fretHandMute="0" highDensity="0" ignore="0" palmMute="0" hopo="0" strum="down">
            //  <chordNote time="83.366" linkNext="0" accent="0" bend="0" fret="3" hammerOn="0" harmonic="0" hopo="0" ignore="0" leftHand="-1" mute="0" palmMute="0" pluck="-1" pullOff="0" slap="-1" slideTo="-1" string="4" sustain="0.000" tremolo="0" harmonicPinch="0" pickDirection="0" rightHand="-1" slideUnpitchTo="-1" tap="0" vibrato="0"/>
            //  <chordNote time="83.366" linkNext="0" accent="0" bend="0" fret="3" hammerOn="0" harmonic="0" hopo="0" ignore="0" leftHand="-1" mute="0" palmMute="0" pluck="-1" pullOff="0" slap="-1" slideTo="-1" string="5" sustain="0.000" tremolo="0" harmonicPinch="0" pickDirection="0" rightHand="-1" slideUnpitchTo="-1" tap="0" vibrato="0"/>
            //</chord>

            // RS1
            //<chord time="83.366" chordId="1" highDensity="0" ignore="0" strum="down"/>
            //<chordTemplate chordName="A" finger0="-1" finger1="0" finger2="1" finger3="1" finger4="1" finger5="-1" fret0="-1" fret1="0" fret2="2" fret3="2" fret4="2" fret5="-1"/>

            // finger > -1 is actual string

            SongNote2014 songNote2014 = new SongNote2014();
            songNote2014.Time = songChord.Time;
            songNote2014.LinkNext = 0;
            songNote2014.Accent = 0;
            songNote2014.Bend = 0;
            songNote2014.Fret = (sbyte)fret;
            songNote2014.HammerOn = 0;
            songNote2014.Hopo = 0;
            songNote2014.Ignore = songChord.Ignore;
            songNote2014.LeftHand = -1;
            songNote2014.Mute = 0;
            songNote2014.PalmMute = 0;
            songNote2014.Pluck = -1;
            songNote2014.PullOff = 0;
            songNote2014.Slap = -1;
            songNote2014.SlideTo = -1;
            songNote2014.String = (byte)gString;
            songNote2014.Sustain = 0.000f;
            songNote2014.Tremolo = 0;
            songNote2014.HarmonicPinch = 0;
            songNote2014.PickDirection = 0;
            songNote2014.RightHand = -1;
            songNote2014.SlideUnpitchTo = -1;
            songNote2014.Tap = 0;
            songNote2014.Vibrato = 0;

            return songNote2014;
        }
        # endregion

        #region Song Xml File to Song2014 XML File

        public string SongFile2Song2014File(string songFilePath, bool overWrite)
        {
            Song2014 song2014 = new Song2014();
            using (var obj = new Rs1Converter())
                song2014 = obj.SongToSong2014(Song.LoadFromFile(songFilePath));

            if (!overWrite)
            {
                var srcDir = Path.GetDirectoryName(songFilePath);
                var srcName = Path.GetFileNameWithoutExtension(songFilePath);
                var backupSrcPath = String.Format("{0}_{1}.xml", Path.Combine(srcDir, srcName), "RS1");

                // backup original RS1 file
                File.Copy(songFilePath, backupSrcPath);
            }

            // write converted RS1 file
            using (FileStream stream = new FileStream(songFilePath, FileMode.Create))
                song2014.Serialize(stream, true);

            return songFilePath;
        }

        #endregion

        #region RS1 Tone to RS2 Tone2014

        public Tone2014 ToneToTone2014(Tone rs1Tone)
        {
            Tone2014 tone2014 = new Tone2014();
            Pedal2014 amp = new Pedal2014();
            Pedal2014 cabinet = new Pedal2014();
            Pedal2014 prepedal1 = new Pedal2014();
            Pedal2014 rack1 = new Pedal2014();
            Pedal2014 rack2 = new Pedal2014();
            tone2014.ToneDescriptors = new List<string>();
            tone2014.Name = rs1Tone.Name;
            tone2014.Key = rs1Tone.Key ?? "";
            tone2014.Volume = rs1Tone.Volume;
            tone2014.IsCustom = true;
            tone2014.NameSeparator = " - ";
            tone2014.SortOrder = 0;

            // setup tone approximation conversions based on rs1Tone.Name
            // based on some real tone combinations found in RS2 CDLC

            if (rs1Tone.Name.ToUpper().Contains(" LEAD"))
            {
                tone2014.ToneDescriptors.Add("$[35724]LEAD");
                amp.Type = "Amps";
                amp.Category = "Amp";
                amp.PedalKey = "Amp_AT120";
                cabinet.Type = "Cabinets";
                cabinet.Category = "Dynamic_Cone";
                cabinet.PedalKey = "Cab_OrangePPC412_57_Cone";
                rack1.Type = "Racks";
                rack1.Category = "Filter";
                rack1.PedalKey = "Rack_StudioEQ";
                prepedal1.Type = "Pedals";
                prepedal1.Category = "Distortion";
                prepedal1.PedalKey = "Pedal_GermaniumDrive";

                tone2014.GearList = new Gear2014()
                {
                    Amp = amp,
                    Cabinet = cabinet,
                    Rack1 = rack1,
                    PrePedal1 = prepedal1
                };
            }

            if (rs1Tone.Name.ToUpper().Contains(" DIS"))
            {
                tone2014.ToneDescriptors.Add("$[35722]DISTORTION");
                amp.Type = "Amps";
                amp.Category = "Amp";
                amp.PedalKey = "Amp_GB100";
                cabinet.Type = "Cabinets";
                cabinet.Category = "Dynamic_Cone";
                cabinet.PedalKey = "Cab_GB412CMKIII_57_Cone";
                rack1.Type = "Racks";
                rack1.Category = "Filter";
                rack1.PedalKey = "Rack_StudioEQ";
                rack2.Type = "Racks";
                rack2.Category = "Reverb";
                rack2.PedalKey = "Rack_StudioVerb";
                prepedal1.Type = "Pedals";
                prepedal1.Category = "Distortion";
                prepedal1.PedalKey = "Pedal_GermaniumDrive";

                tone2014.GearList = new Gear2014()
                {
                    Amp = amp,
                    Cabinet = cabinet,
                    Rack1 = rack1,
                    Rack2 = rack2,
                    PrePedal1 = prepedal1
                };
            }

            if (rs1Tone.Name.ToUpper().Contains(" CLEAN"))
            {
                tone2014.ToneDescriptors.Add("$[35720]CLEAN");
                amp.Type = "Amps";
                amp.Category = "Amp";
                amp.PedalKey = "Amp_TW40";
                cabinet.Type = "Cabinets";
                cabinet.Category = "Dynamic_Cone";
                cabinet.PedalKey = "Cab_TW112C_57_Cone";
                rack1.Type = "Racks";
                rack1.Category = "Filter";
                rack1.PedalKey = "Rack_StudioEQ";

                tone2014.GearList = new Gear2014()
                {
                    Amp = amp,
                    Cabinet = cabinet,
                    Rack1 = rack1
                };
            }

            if (rs1Tone.Name.ToUpper().Contains(" ACOUSTIC"))
            {
                tone2014.ToneDescriptors.Add("$[35721]ACOUSTIC");
                amp.Type = "Amps";
                amp.Category = "Amp";
                amp.PedalKey = "Amp_TW40";
                cabinet.Type = "Cabinets";
                cabinet.Category = "Dynamic_Cone";
                cabinet.PedalKey = "Cab_GB412CMKIII_57_Cone";
                rack1.Type = "Racks";
                rack1.Category = "Filter";
                rack1.PedalKey = "Rack_StudioEQ";
                rack2.Type = "Racks";
                rack2.Category = "Dynamics";
                rack2.PedalKey = "Rack_StudioCompressor";
                prepedal1.Type = "Pedals";
                prepedal1.Category = "Filter";
                prepedal1.PedalKey = "Pedal_AcousticEmulator";

                tone2014.GearList = new Gear2014()
                {
                    Amp = amp,
                    Cabinet = cabinet,
                    Rack1 = rack1,
                    Rack2 = rack2,
                    PrePedal1 = prepedal1
                };
            }

            if (rs1Tone.Name.ToUpper().Contains(" BASS"))
            {
                tone2014.ToneDescriptors.Add("$[35715]BASS");
                amp.Type = "Amps";
                amp.Category = "Amp";
                amp.PedalKey = "Bass_Amp_CH300B";
                cabinet.Type = "Cabinets";
                cabinet.Category = "Dynamic_Cone";
                cabinet.PedalKey = "Bass_Cab_BT410BC_57_Cone";
                rack1.Type = "Racks";
                rack1.Category = "Filter";
                rack1.PedalKey = "Rack_StudioEQ";

                tone2014.GearList = new Gear2014()
                {
                    Amp = amp,
                    Cabinet = cabinet,
                    Rack1 = rack1
                };
            }

            return tone2014;
        }

        #endregion

        public void Dispose() { }

    }
}
