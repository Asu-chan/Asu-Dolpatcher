using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

/*******************************/
/*** Asu's Dolpatcher v1.0.0 ***/
/* This file contains the main */
/****** code of the tool. ******/
/*******************************/
/* Everything you find here is */
/**** free to redistribute, ****/
/* edit and publish as long as */
/***** credits are given. ******/
/*******************************/


namespace Asu_s_Dolpatcher
{
	class Program
	{

		[STAThread] // This is needed for System.Windows.Forms to work. Sorry MacOS/Linux users. :/
		static void Main(string[] args)
		{
			Console.WriteLine("Asu's Dolpatcher - v1.0.0");
			Dolpatcher dp = new Dolpatcher();
			dp.Main(args);
		}
	}
	public class Dolpatcher
	{
		List<uint> dolOffsets = new List<uint>();
		List<uint> realPointers = new List<uint>();
		List<uint> sectionSizes = new List<uint>();

		public void Main(string[] args)
		{
			bool isSilent = false, alwaysCreateSections = false, neverCreateSections = false, isThereDefinedPaths = false;
			string dolPath = "", xmlPath = "", outPath = "", binPath = "";
			if (args.Length > 0) // Arguments getting
			{
				if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
				{
					Console.WriteLine("A tool to patch Nintendo Wii DOL executables using Riivolution XML files.\r\n\r\n");
					Console.WriteLine("Usage: \"Asu's Dolpatcher.exe\" <DOL path> <Riivolution XML path> <Output DOL path> [options]");
					Console.WriteLine("       \"Asu's Dolpatcher.exe\" [options]");
					Console.WriteLine("       \"Asu's Dolpatcher.exe\"");
					Console.WriteLine("       In the 2nd and 3rd cases, you will be asked for the file paths.\r\n\r\n"); // Thanks to Mullkaw for correcting my weird-sounding english! ^^
					Console.WriteLine("Options: --silent                  -> Prevents from displaying any console outputs aparts from the necessary ones");
					Console.WriteLine("         --always-create-sections  -> Always create a new section if a target pointer is outside of the DOL range.");
					Console.WriteLine("         --never-create-sections   -> Never create a new section if a target pointer is outside of the DOL range.");
					Console.WriteLine("         --binary-files-dir <path> -> Set the default directory for binary files.");
					return;
				}

				if (args.Contains("--silent"))
				{
					isSilent = true;
					Console.WriteLine("Silent Mode: true");
				}

				if (args.Contains("--always-create-sections"))
				{
					alwaysCreateSections = true;
				}

				if (args.Contains("--never-create-sections"))
				{
					neverCreateSections = true;
				}

				if (args.Contains("--binary-files-dir"))
				{
					binPath = args[Array.IndexOf(args, "--binary-files-dir") + 1];
					if(!binPath.EndsWith(Path.DirectorySeparatorChar.ToString())) { binPath += Path.DirectorySeparatorChar; }
				}

				if (alwaysCreateSections && neverCreateSections)
				{
					Console.WriteLine("PLEASE. You can't want new sections AND not wanting them! Either you want them or either you don't!!");
					return;
				}

				if (!isSilent)
				{
					Console.WriteLine("Arguments got:\r\n-Section Creating: " + (alwaysCreateSections ? "Always" : "Never"));
				}

				if (args[0].Contains(".dol"))
				{
					dolPath = args[0];
					xmlPath = args[1];
					outPath = args[2];
					isThereDefinedPaths = true;
				}
			}

			if (isThereDefinedPaths) // Paths are already defined? Directly do the stuff.
			{
				if (!File.Exists(dolPath) || !File.Exists(xmlPath))
				{
					Console.WriteLine("Can't find DOL or XML file: No such file or directory.");
				}
				doStuff(dolPath, xmlPath, outPath, binPath, isSilent, alwaysCreateSections, neverCreateSections);
				return;
			}
			else // No paths defined? Ask them.
			{
				// Getting DOL path
				Console.WriteLine("Please select a DOL file to patch.");
				using (OpenFileDialog dialog = new OpenFileDialog())
				{
					dialog.Filter = "Nintendo Wii Executable|*.dol|All files (*.*)|*.*";
					dialog.FilterIndex = 1;
					dialog.RestoreDirectory = true;
					if (dialog.ShowDialog() == DialogResult.OK)
					{
						Console.WriteLine("Please select a Riivolution XML file.");

						// Getting XML path
						using (OpenFileDialog dialog2 = new OpenFileDialog())
						{
							dialog2.Filter = "Riivolution Extensible Markup Language File|*.xml|All files (*.*)|*.*";
							dialog2.FilterIndex = 1;
							dialog2.RestoreDirectory = true;
							if (dialog2.ShowDialog() == DialogResult.OK)
							{
								Console.WriteLine("Please select where do you want your DOL file to be saved.");

								// Getting output DOL path
								SaveFileDialog textDialog;
								textDialog = new SaveFileDialog();
								textDialog.Filter = "Nintendo Wii Executable|*.dol|All files (*.*)|*.*";
								if (textDialog.ShowDialog() == DialogResult.OK)
								{
									System.IO.Stream fileStream = textDialog.OpenFile();
									System.IO.StreamWriter sw = new System.IO.StreamWriter(fileStream);
									outPath = ((FileStream)(sw.BaseStream)).Name;
									sw.Close();

									doStuff(dialog.FileName, dialog2.FileName, outPath, binPath, isSilent, alwaysCreateSections, neverCreateSections);
								}
								else
								{
									Console.WriteLine("DOL saving cancelled. Closing...");
									return;
								}
							}
							else
							{
								Console.WriteLine("XML selecting cancelled. Closing...");
								return;
							}
						}
					}
					else
					{
						Console.WriteLine("DOL selecting cancelled. Closing...");
						return;
					}
				}
			}
		}

		public void doStuff(string dolPath, string xmlPath, string outPath, string binPath, bool isSilent, bool alwaysCreateSections, bool neverCreateSections)
		{

			byte[] dol = File.ReadAllBytes(dolPath);
			List<string> xml = File.ReadAllLines(xmlPath).ToList();

			if (!isSilent) { Console.WriteLine("Files found."); Console.WriteLine("Decoding DOL file header..."); }

			for (int i = 0; i < 0x48; i += 4)
			{
				dolOffsets.Add(BitConverter.ToUInt32(dol.Skip(i).Take(4).Reverse().ToArray(), 0));
				realPointers.Add(BitConverter.ToUInt32(dol.Skip(i + 0x48).Take(4).Reverse().ToArray(), 0));
				sectionSizes.Add(BitConverter.ToUInt32(dol.Skip(i + 0x90).Take(4).Reverse().ToArray(), 0));
			}

			if (!isSilent) { Console.WriteLine("Decoding XML file..."); }

			// I know this is a pretty bad way to do it, but it works.
			string fullXML = "<riivomem>\r\n";
			foreach (string line in xml) // Removing unnecessary lines.
			{
				if (!line.Contains("memory"))
				{
					continue;
				}
				fullXML += line + "\r\n";
			}
			fullXML += "</riivomem>";

			XmlRootAttribute xRoot = new XmlRootAttribute();
			xRoot.ElementName = "riivomem";
			xRoot.IsNullable = true;

			XmlSerializer serializer = new XmlSerializer(typeof(riivoPatches), xRoot);
			byte[] xmlButInBytes = Encoding.UTF8.GetBytes(fullXML);
			MemoryStream stream = new MemoryStream(xmlButInBytes);
			riivoPatches patches = (riivoPatches)serializer.Deserialize(stream);

			if (!isSilent) { Console.WriteLine("-Found " + patches.memory.Length + " patches.\r\n\r\nPatching..."); }

			// Patching the DOL!
			foreach (riivoPatch riivo in patches.memory)
			{
				uint pointer = Convert.ToUInt32(riivo.offset.Replace("0x", ""), 16);
				uint dolOffset = getOffsetFromPointer(pointer);

				if (riivo.valuefile == "" || riivo.valuefile == null) // Classic memory patches
				{
					if (dolOffset == 0) // I didn't implement the section creating feature for single patches as it would literally create one section PER patch that is outside of the DOL range, which would end up using all sections with only a very few patches.
					{
						if (!isSilent) { Console.WriteLine(riivo.offset + " is outside of the DOL range, skipping to the next patch."); }
						continue;
					}

					int byteCount = riivo.value.Length / 2;

					if (riivo.original != "" && riivo.original != null) // Compare to the original data in the DOL in case it's needed
					{
						string OGValue = "";

						for (int i = 0; i < byteCount; i++)
						{
							OGValue += dol[dolOffset + i].ToString("X2");
						}

						if (OGValue.ToUpper() != riivo.original.ToUpper()) // I know comparing these as strings is definitely not the best idea, but I can't think of any better way.
						{
							if (!isSilent) { Console.WriteLine(riivo.offset + " -> OG: " + riivo.original + "; DOL: " + OGValue + " -> Doesn't match, ignoring..."); }
							continue;
						}
					}

					for (int i = 0; i < byteCount; i++) // Replace the bytes in the dol
					{
						dol[dolOffset + i] = Convert.ToByte(riivo.value.Substring(i * 2, 2), 16);
					}

					if (riivo.original == "" || riivo.original == null) // This is purely for "Console outputs are looking good" purposes
					{
						riivo.original = "None";
					}

					if (!isSilent) { Console.WriteLine(riivo.offset + " -> DOLOffset: " + dolOffset + "; Value: " + riivo.value + "; OG: " + riivo.original + " -> Patched."); }

					continue;
				}
				else // Memory patches involving a binary file
				{
					string path = (binPath == "") ? (Path.GetDirectoryName(xmlPath) + Path.DirectorySeparatorChar + riivo.valuefile) : (binPath + riivo.valuefile);
					byte[] binaryFile = new byte[0];

					if (File.Exists(path))
					{
						binaryFile = File.ReadAllBytes(path);
						if (!isSilent) { Console.WriteLine("Found " + riivo.valuefile); }
					}

					else
					{
						Console.WriteLine("Cannot find " + riivo.valuefile + ", please select it.");
						using (OpenFileDialog dialog3 = new OpenFileDialog())
						{
							dialog3.Filter = "Binary File|*.bin|All files (*.*)|*.*";
							dialog3.FilterIndex = 1;
							dialog3.RestoreDirectory = true;

							if (dialog3.ShowDialog() == DialogResult.OK)
							{
								binaryFile = File.ReadAllBytes(dialog3.FileName);
								if (!isSilent) { Console.WriteLine("Found " + riivo.valuefile + ", continuing..."); }
							}
							else
							{
								if (!isSilent) { Console.WriteLine("Ignoring Patch " + riivo.value + " -> Binary file selecting cancelled."); }
							}
						}
					}

					if (dolOffset == 0) // New section creating
					{
						string answer;
						if (alwaysCreateSections == false && neverCreateSections == false)
						{
							Console.Write(riivo.offset + " is outside of the DOL range. Do you want to try to create a new section for it? (Yes/No): ");
						}
						answer = alwaysCreateSections ? "yes" : (neverCreateSections ? "no" : Console.ReadLine());

						if (answer.Equals("yes", StringComparison.InvariantCultureIgnoreCase) || answer.Equals("y", StringComparison.InvariantCultureIgnoreCase))
						{

							int newSectionIndex = getNextEmptySection();

							if (newSectionIndex < 0)
							{
								Console.WriteLine("Can't create a new section for pointer " + riivo.offset + ": No free section available.");
								continue;
							}

							int highestSection = getHighestSection();
							byte[] previousSections = dol.Take((int)(dolOffsets[highestSection] + sectionSizes[highestSection])).ToArray();
							byte[] footer = dol.Skip((int)(dolOffsets[highestSection] + sectionSizes[highestSection])).ToArray();
							dol = previousSections.Concat(binaryFile).Concat(footer).ToArray();
							dolOffsets[newSectionIndex] = dolOffsets[highestSection] + sectionSizes[highestSection];
							realPointers[newSectionIndex] = pointer;
							sectionSizes[newSectionIndex] = (uint)binaryFile.Length;

							for (int i = 0; i < 4; i++)
							{
								dol[4 * newSectionIndex + i] = Convert.ToByte(dolOffsets[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
								dol[4 * newSectionIndex + i + 0x48] = Convert.ToByte(realPointers[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
								dol[4 * newSectionIndex + i + 0x90] = Convert.ToByte(sectionSizes[newSectionIndex].ToString("X8").Substring(i * 2, 2), 16);
							}

							dolOffset = getOffsetFromPointer(pointer);

							if (!isSilent) { Console.WriteLine("Created new section for " + riivo.valuefile + " at " + dolOffset.ToString("X") + ", pointing at " + riivo.offset + " at index " + newSectionIndex + "."); }
						}
						else
						{
							if (!isSilent) { Console.WriteLine("New section creating refused, skipping to next patch."); }
							continue;
						}
					}

					for (int i = 0; i < binaryFile.Length; i++)
					{
						dol[dolOffset + i] = binaryFile[i];
					}

					if (riivo.original == "" || riivo.original == null)
					{
						riivo.original = "None";
					}

					if (!isSilent) { Console.WriteLine(riivo.offset + " -> DOLOffset: " + dolOffset + "; ValueFile: " + riivo.valuefile + " -> Patched."); }
				}

			}

			Console.WriteLine("DOL patched.");

			using (FileStream outDol = new FileStream(outPath, FileMode.Create))
			{
				for (int i = 0; i < dol.Length; i++)
				{
					outDol.WriteByte(dol[i]);
				}
			}

			if (!isSilent) { Console.WriteLine("DOL Saved! Closing..."); }
		}

		public uint getOffsetFromPointer(uint pointer)
		{
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (pointer >= realPointers[i] && pointer <= (realPointers[i] + sectionSizes[i]))
				{
					return dolOffsets[i] + (pointer - realPointers[i]);
				}
			}

			return 0;
		}

		public int getHighestSection()
		{
			int sectionIndex = 0;
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (dolOffsets[i] > dolOffsets[sectionIndex])
				{
					sectionIndex = i;
				}
			}
			return sectionIndex;
		}

		public int getNextEmptySection()
		{
			for (int i = 0; i < dolOffsets.Count; i++)
			{
				if (dolOffsets[i] == 0)
				{
					return i;
				}
			}
			return -1;
		}
	}

	public class riivoPatches
	{
		[XmlElement(ElementName = "memory")]
		public riivoPatch[] memory { get; set; }
	}

	public class riivoPatch
	{
		[XmlAttribute(AttributeName = "offset")]
		public string offset { get; set; }

		[XmlAttribute(AttributeName = "value")]
		public string value { get; set; }

		[XmlAttribute(AttributeName = "original")]
		public string original { get; set; }

		[XmlAttribute(AttributeName = "valuefile")]
		public string valuefile { get; set; }
	}
}
