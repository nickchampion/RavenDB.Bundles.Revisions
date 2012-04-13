properties {
	$base_dir  = resolve-path ..\
	$build_dir = "$base_dir\build"
	$src_dir = "$base_dir\src"
	$sln_file = "$base_dir\src\RavenDB.Bundles.Revisions.sln"
	$version = "1.0"
	$tools_dir = "$base_dir\tools"
	$buildNumber = 0
}

task default -depends Release

task Clean {
	remove-item -force -recurse $build_dir -ErrorAction SilentlyContinue
	exec { msbuild $sln_file /t:Clean }
}

task Compile -depends Clean {
	exec { msbuild $sln_file /p:Configuration=Release }
}

task RunTests -depends Compile {
	New-Item $build_dir -Type directory
	.$tools_dir\XunitRunner\xunit.console.clr4.exe "$base_dir\src\Tests.Raven.Bundles.Revisions\bin\Release\Tests.Raven.Bundles.Revisions.dll" /html "$build_dir\index.html"
}

task BuildPackages -depends Compile{
	New-Item $build_dir\RavenDB.Bundles.Revisions\lib\net40 -Type directory | Out-Null
	Copy-Item $base_dir\NuGet\RavenDB.Bundles.Revisions.nuspec $build_dir\RavenDB.Bundles.Revisions\RavenDB.Bundles.Revisions.nuspec
	@("Raven.Bundles.Revisions.???") |% { Copy-Item "$src_dir\Raven.Bundles.Revisions\bin\Release\$_" $build_dir\RavenDB.Bundles.Revisions\lib\net40 }

	New-Item $build_dir\RavenDB.Client.Revisions\lib\net40 -Type directory | Out-Null
	Copy-Item $base_dir\NuGet\RavenDB.Client.Revisions.nuspec $build_dir\RavenDB.Client.Revisions\RavenDB.Client.Revisions.nuspec
	@("Raven.Client.Revisions.???") |% { Copy-Item "$src_dir\Raven.Client.Revisions\bin\Release\$_" $build_dir\RavenDB.Client.Revisions\lib\net40 }
	
	$packagesConfig = [xml](Get-Content $base_dir\src\Raven.Bundles.Revisions\packages.config)
	$packagesConfig.packages.package | Where-Object {$_.id -eq "RavenDB.Database"} |% {$ravenDependencyVersion = $_.Version}
	$packageVersion = $ravenDependencyVersion + "." + $buildNumber
	
	$packages = Get-ChildItem $build_dir *.nuspec -recurse
	$packages |% { 
		$nuspec = [xml](Get-Content $_.FullName)
		$nuspec.package.metadata.version = $packageVersion
		$nuspec | Select-Xml '//dependency' |% {
			if($_.Node.Id.StartsWith('RavenDB')){
				$_.Node.Version = "[$ravenDependencyVersion]"
			}
		}
		$nuspec.Save($_.FullName);
		&"$base_dir\src\.nuget\nuget.exe" pack $_.FullName -OutputDirectory $build_dir
	}
}

task Release -depends RunTests, BuildPackages