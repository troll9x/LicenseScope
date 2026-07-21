[CmdletBinding()] param([Parameter(Mandatory)][string]$Path)
$resolved=(Resolve-Path -LiteralPath $Path).Path;$stream=[IO.File]::OpenRead($resolved)
try{$reader=[IO.BinaryReader]::new($stream);$stream.Position=0x3c;$peOffset=$reader.ReadInt32();$stream.Position=$peOffset;if($reader.ReadUInt32()-ne 0x4550){throw 'Invalid PE signature'};$machine=$reader.ReadUInt16();$name=switch($machine){0x14c{'X86'}0x8664{'X64'}0xaa64{'ARM64'}default{"Unknown-0x$($machine.ToString('X4'))"}};[pscustomobject]@{Path=$resolved;Machine=$name;MachineCode=('0x{0:X4}' -f $machine)}}finally{$stream.Dispose()}
