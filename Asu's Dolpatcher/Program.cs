using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

/*******************************/
/*** Asu's Dolpatcher v1.1.0 ***/
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
			Console.WriteLine("Asu's Dolpatcher - v1.1.0");
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
			string dolPath = "", xmlPath = "", outPath = "", binPath = "", region = "Ask";
			List<string> patches = new List<string>();

			if (args.Length > 0) // Arguments getting
			{
				if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?")
				{
					Console.WriteLine("A tool to patch Nintendo Wii DOL executables using Riivolution XML files.\r\n\r\n");
					Console.WriteLine("Usage: \"Asu's Dolpatcher.exe\" <DOL path> <Riivolution XML path> <Output DOL path> [options]");
					Console.WriteLine("       \"Asu's Dolpatcher.exe\" [options]");
					Console.WriteLine("       \"Asu's Dolpatcher.exe\"");
					Console.WriteLine("       In the 2nd and 3rd cases, you will be asked for the file paths.\r\n\r\n"); // Thanks to Mullkaw for correcting my weird-sounding english! ^^
					Console.WriteLine("Options: --silent                             -> Prevents from displaying any console outputs aparts from the necessary ones");
					Console.WriteLine("         --always-create-sections             -> Always create a new section if a target pointer is outside of the DOL range.");
					Console.WriteLine("         --never-create-sections              -> Never create a new section if a target pointer is outside of the DOL range.");
					Console.WriteLine("         --binary-files-dir <path>            -> Set the default directory for binary files.");
					Console.WriteLine("         --only-patches \"Patch1;Patch2;...\" -> Makes it so only the given patches will be used to patch the DOL.");
					Console.WriteLine("         --region <P/E/J/K/W>                 -> Uses the said region for files that requires a region name.");
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

				if (args.Contains("--only-patches"))
				{
					string patchesArg = args[Array.IndexOf(args, "--only-patches") + 1];
					if (patchesArg.Contains(";"))
					{
						patches = args[Array.IndexOf(args, "--only-patches") + 1].Split(';').ToList();
					}
					else
					{
						patches.Add(patchesArg);
					}
				}

				if (args.Contains("--region"))
				{
					region = args[Array.IndexOf(args, "--region") + 1];
					if(region != "E" && region != "P" && region != "J" && region != "K" && region != "W")
					{
						Console.WriteLine(region + " isn't a valid region. Please choose between P, E, J, K or W.");
						return;
					}
				}

				if (alwaysCreateSections && neverCreateSections)
				{
					Console.WriteLine("PLEASE. You can't want new sections AND not wanting them! Either you want them or either you don't!!");
					return;
				}

				if (!isSilent)
				{
					Console.WriteLine("Arguments got:");
					Console.WriteLine("-Section Creating: " + (alwaysCreateSections ? "Always" : (neverCreateSections ? "Never" : "Ask")));
					Console.WriteLine("-Region: " + region);

					if(patches.Count > 0)
					{
						string allPatches = "";
						foreach (string patch in patches)
						{
							allPatches += "\"" + patch + "\"" + ((patches.IndexOf(patch) == patches.Count - 1) ? "" : ", ");
						}
						Console.WriteLine("-Patches: " + allPatches);
					}
					else
					{
						Console.WriteLine("-Patches: All");
					}

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
				doStuff(dolPath, xmlPath, outPath, binPath, patches, region, isSilent, alwaysCreateSections, neverCreateSections);;
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

									doStuff(dialog.FileName, dialog2.FileName, outPath, binPath, patches, region, isSilent, alwaysCreateSections, neverCreateSections);
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

		List<int> newSectionsIndexes = new List<int>();
		byte[] dol = new byte[0];
		public void doStuff(string dolPath, string xmlPath, string outPath, string binPath, List<string> patchesList, string region, bool isSilent, bool alwaysCreateSections, bool neverCreateSections)
		{
			dol = File.ReadAllBytes(dolPath);

			if (!isSilent) { Console.WriteLine("Files found."); Console.WriteLine("Decoding DOL file header..."); }

			for (int i = 0; i < 0x48; i += 4)
			{
				dolOffsets.Add(BitConverter.ToUInt32(dol.Skip(i).Take(4).Reverse().ToArray(), 0));
				realPointers.Add(BitConverter.ToUInt32(dol.Skip(i + 0x48).Take(4).Reverse().ToArray(), 0));
				sectionSizes.Add(BitConverter.ToUInt32(dol.Skip(i + 0x90).Take(4).Reverse().ToArray(), 0));
			}

			if (!isSilent) { Console.WriteLine("Decoding XML file..."); }

			List<riivolutionPatch> patches = new List<riivolutionPatch>();
			riivolutionXML XML = riivolutionXML.load(xmlPath);
			if(patchesList.Count > 0)
			{
				foreach(string patchToUse in patchesList)
				{
					int index = XML.findPatchIndexByName(patchToUse);
					if(index < 0)
					{
						Console.WriteLine("Can't find patch " + patchToUse);
						return;
					}
					patches.Add(XML.patch[index]);
				}
			}
			else
			{
				patches = XML.patch.ToList();
			}

			List<string> supportedRegions = new List<string>();
			foreach (riivolutionIDRegion available in XML.id.region)
			{
				supportedRegions.Add(available.type);
			}
			if(region != "Ask" && !supportedRegions.Contains(region))
			{
				Console.WriteLine("Region \"" + region + "\" isn't supported by this XML.");
			}


			foreach (riivolutionPatch patch in patches)
			{
				if (!isSilent) { Console.WriteLine("-Found " + patch.memory.Length + " dolpatches.\r\n\r\nPatching..."); }

				// Patching the DOL!
				foreach (riivolutionPatchMemory riivo in patch.memory)
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

						if(riivo.target != "" && riivo.target != null)
						{
							if (region == "Ask")
							{
								string regionsAvailable = "";
								foreach (string available in supportedRegions)
								{
									regionsAvailable += available + "/";
								}
								Console.Write("A region is required to continue, please input a region to use (" + regionsAvailable.Substring(0, regionsAvailable.Length - 1) + "): ");

								while (true)
								{
									string outRegion = Console.ReadLine();
									if (supportedRegions.Contains(outRegion))
									{
										region = outRegion;
										break;
									}
									else
									{
										Console.Write(outRegion + " isn't a valid region. Please input a region to use (" + regionsAvailable.Substring(0, regionsAvailable.Length - 1) + "): ");
									}
								}
							}

							if(riivo.target != region)
							{
								Console.WriteLine("Patch " + riivo.offset + " can't be applied to this region, skipping to the next patch.");
								continue;
							}
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
						if (riivo.valuefile.Contains("{$__region}"))
						{
							if(region != "Ask")
							{
								if (!isSilent) { Console.WriteLine(riivo.valuefile + " is a region-changing file, using " + riivo.valuefile.Replace("{$__region}", region)); }
								riivo.valuefile = riivo.valuefile.Replace("{$__region}", region);
							}
							else
							{
								string regionsAvailable = "";
								foreach(string available in supportedRegions)
								{
									regionsAvailable += available + "/";
								}
								Console.Write(riivo.valuefile + " is a region-changing file, please input a region to use (" + regionsAvailable.Substring(0, regionsAvailable.Length - 1) + "): ");

								while(true)
								{
									string outRegion = Console.ReadLine();
									if(supportedRegions.Contains(outRegion))
									{
										riivo.valuefile = riivo.valuefile.Replace("{$__region}", outRegion);
										break;
									}
									else
									{
										Console.Write(outRegion + " isn't a valid region. Please input a region to use (" + regionsAvailable.Substring(0, regionsAvailable.Length - 1) + "): ");
									}
								}
							}
						}


						string path = (binPath == "") ? (Path.GetDirectoryName(xmlPath) + Path.DirectorySeparatorChar + riivo.valuefile.Replace('/', '\\')) : (binPath + riivo.valuefile.Replace('/', '\\'));
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

								newSectionsIndexes.Add(newSectionIndex);

								dolOffset = getOffsetFromPointer(pointer);

								if (!isSilent) { Console.WriteLine("Created new section for " + riivo.valuefile + " at " + dolOffset.ToString("X") + ", pointing at " + riivo.offset + " at index " + newSectionIndex + "."); }

								if(mergeSectionsThatCanBeMerged())
								{
									Console.WriteLine("Merged sections that could be merged.");
								}
							}
							else
							{
								if (!isSilent) { Console.WriteLine("New section creating refused, skipping to next patch."); }
								continue;
							}
						}
						else
						{

							for (int i = 0; i < binaryFile.Length; i++)
							{
								dol[dolOffset + i] = binaryFile[i];
							}
						}

						if (riivo.original == "" || riivo.original == null)
						{
							riivo.original = "None";
						}

						if (!isSilent) { Console.WriteLine(riivo.offset + " -> DOLOffset: " + dolOffset.ToString("X") + "; ValueFile: " + riivo.valuefile + " -> Patched."); }
					}
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
				if (pointer >= realPointers[i] && pointer < (realPointers[i] + sectionSizes[i]))
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

		public bool mergeSectionsThatCanBeMerged()
		{
			bool didMerged = false;
			foreach(int newSection in newSectionsIndexes) {
				foreach (int otherSection in newSectionsIndexes)
				{
					if(otherSection == newSection)
					{
						continue;
					}
					if(realPointers[newSection] + sectionSizes[newSection] == realPointers[otherSection])
					{
						sectionSizes[newSection] += sectionSizes[otherSection];
						dolOffsets[otherSection] = 0;
						realPointers[otherSection] = 0;
						sectionSizes[otherSection] = 0;
						didMerged = true;
					}
				}
			}
			reencodeDolHeader();
			return didMerged;
		}

		public void reencodeDolHeader()
		{
			for(int i = 0; i < dolOffsets.Count; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					dol[4 * i + j] = Convert.ToByte(dolOffsets[i].ToString("X8").Substring(j * 2, 2), 16);
					dol[4 * i + j + 0x48] = Convert.ToByte(realPointers[i].ToString("X8").Substring(j * 2, 2), 16);
					dol[4 * i + j + 0x90] = Convert.ToByte(sectionSizes[i].ToString("X8").Substring(j * 2, 2), 16);
				}
			}
		}
	}
}
