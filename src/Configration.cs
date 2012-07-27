﻿/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 2012/7/17
 * Time: 17:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace UmengChannel
{
	public class Configration{
		//project directory
		public const string projects_path = "projects";
		public const string general_setting = "sys.cfg";
		
		public string java_home {get;set;}
		public string android_home {get;set;}
		public string ant_home {get;set;}
		
		private bool hasSetEnvironment = false;
		
		private string currentProject;
		public SortedList<string,ProjectConfigration> projects {get ;set;}
		
		private static Configration configration;
		
		private Configration(){
			if(projects == null){
				projects = new SortedList<string, ProjectConfigration>();
				
			}
			
			ant_home = Path.Combine(System.Environment.CurrentDirectory,Path.Combine("tools","ant"));
			buildDirectory();
			
			loadSysConfig();
			LoadProjects();
		}
		
		private void buildDirectory()
		{
			string pro_path = Path.Combine(System.Environment.CurrentDirectory, "projects");
			
			if(!Directory.Exists(pro_path))
			{
				Directory.CreateDirectory(pro_path);
			}
		}
		
		public void setEnvironment(){
			if(hasSetEnvironment)
			{
				return;
			}
			
			string s1 = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
			string s2 = System.Environment.GetEnvironmentVariable("JAVA_HOME", EnvironmentVariableTarget.Process);
			string s3 = System.Environment.GetEnvironmentVariable("ANT_HOME",EnvironmentVariableTarget.Process);
			
			Log.i("set environment");
			System.Environment.SetEnvironmentVariable("JAVA_HOME", java_home);//, EnvironmentVariableTarget.User);
			System.Environment.SetEnvironmentVariable("ANT_HOME", ant_home);//, EnvironmentVariableTarget.User);
			
			System.Text.StringBuilder path = new System.Text.StringBuilder();
			
			path.Append(Path.Combine("%JAVA_HOME%","bin"));
			path.Append(";");
			path.Append(Path.Combine("%JAVA_HOME%","lib"));
			path.Append(";");
			path.Append(Path.Combine(android_home,"tools"));
			path.Append(";");
			path.Append(Path.Combine("%ANT_HOME%","bin"));
			path.Append(";");
			path.Append("%JAVA_HOME%;");
			path.Append("%ANT_HOME%;");
			path.Append(Path.Combine(java_home,"bin;"));//+
			path.Append(Path.Combine(ant_home,"bin;"));//+
			
			System.Environment.SetEnvironmentVariable("PATH", path.ToString());//, EnvironmentVariableTarget.User);
			
			hasSetEnvironment = true;
			
			            
		}
		
		public static Configration Instanse(){
			if(configration == null) {
				Utils.checkOrSetAnthome();
				configration = new Configration();
			}
			
			return configration;
		}
		
		private void loadSysConfig(){
			string sys_setting = Path.Combine("projects",general_setting);
			if(!File.Exists(sys_setting)) return;
			
			using(StreamReader sw = File.OpenText(sys_setting))
		    {
				java_home = sw.ReadLine();
				android_home = sw.ReadLine();
		    }
			
			if(!string.IsNullOrEmpty(java_home) && !string.IsNullOrEmpty(android_home))
			{
				setEnvironment();
			}
		}

		/// <summary>
		/// if the new project exits, return and checkout the old project
		/// else if the generate a new project!
		/// </summary>
		/// <param name="new_project"></param>
		/// <returns></returns>
		public ProjectConfigration addProject(string new_project){
			currentProject = new_project;
			
			if(projects.ContainsKey(new_project)){
				return getCurrentProjectConfig();
			}
			
			ProjectConfigration project =  new ProjectConfigration();
			project.writeSettintToFile(new_project);
			projects.Add(new_project, project);
			
			return project;	
		}
		
		public ProjectConfigration updateCurrentProject(string selectedProject){
			if(currentProject != selectedProject){
				currentProject = selectedProject;
				loadConfigration();
			}	
			
			return getCurrentProjectConfig();
		}
		
		public ProjectConfigration getDefaultProject(){
			ProjectConfigration config = null;
			if(projects!= null && projects.Count > 0){
				config = projects.Values[0] as ProjectConfigration;
			}
			
			if(config == null) config = new ProjectConfigration();
			
			return config;
		}
		
		private void loadConfigration(){
			
		}
		
		public ProjectConfigration getOrCreateProject(string project){
			ProjectConfigration config = null;
			if(projects.ContainsKey(project)){
				config = projects.Values[projects.IndexOfKey(project)];
			}
			if(config == null) {
				config = new ProjectConfigration();
			}
			
			
			return config;
		}
		
		public ProjectConfigration getCurrentProjectConfig(){
			int index = projects.IndexOfKey(currentProject);
			return projects.Values[index] as ProjectConfigration;
		}
		
		public void saveCurrentProject(ProjectConfigration project){
			project.writeSettintToFile(currentProject);
		}
		
		public void saveProjects(){
			if(projects == null || projects.Count<=0) return;
			
			foreach(KeyValuePair<string, ProjectConfigration> pair in projects){
				pair.Value.writeSettintToFile(pair.Key);
			}
		}
		
		public void saveSysConfig(){

			using(StreamWriter sw = File.CreateText(Path.Combine("projects",general_setting))){
				sw.WriteLine(java_home);
				sw.WriteLine( android_home);
			}
		}
		/// <summary>
		/// project.count = 1 : return new ProjectConfigration
		/// project.count > 1 : return next.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public ProjectConfigration deleteProject(int index){
			if(projects.Count < 0 || index < 0 || (index+1 > projects.Count))
			{
				throw new Exception("index out of range");
			}
			
			string project_name  = projects.Keys[index];
			string setting_project_path = Path.Combine(System.Environment.CurrentDirectory,
                Path.Combine("projects", Utils.generateSettingFileName(project_name)));

			
			if( File.Exists(setting_project_path))  File.Delete(setting_project_path);
			
			projects.RemoveAt(index);
			
			ProjectConfigration project = null;
			
			if(projects.Count <= 0) 
			{
				project = new ProjectConfigration();
			}else
			{
				project = projects.Values[0];
			}
				
			return project;
		}
		
		private void LoadProjects(){

			if(!Directory.Exists(projects_path)) return;
			
			string [] files = Directory.GetFiles(projects_path);
			string project_name = null;
  			ProjectConfigration pconfig = null;
  			
  			foreach (string f in files.Where( X=> ((string)X).Contains("setting@")))
			 {
			 	project_name = parceProjectNameFromFileName(f);
			 	pconfig = ProjectConfigration.readSettingFromFile(project_name);
			 	
			 	if(pconfig == null){
			 		File.Delete(f);
			 	}else{
			 		projects.Add(project_name, pconfig);
			 	}
			 	
			 }
		}
		private string parceProjectNameFromFileName(string fileName){
			fileName = System.IO.Path.GetFileName(fileName);
			return fileName.Substring(8, fileName.Length - 12);
		}
		
		
	}
	/// <summary>
	/// Description of Configration.
	/// </summary>
	[Serializable]
	public class ProjectConfigration
	{
		//setting setting@project.xml
		//private const string setting = "setting";
		
		//project
		public string project_path {get;set;}
		
		//sign
		
		public string keystore_file_path {get;set;}
		public string keystore_pw {get;set;}
		public string key_pw {get;set;}
		
		public string alias {get;set;}
		
		// should proguard
		public bool setProguard {get;set;}
		// tools path
		
		public List<string> channels {get;set;}

		public ProjectConfigration()
		{
			channels = new List<string>();
		}
		
		public bool addChannel(string new_channel){
			if(channels.Contains(new_channel)){
				return false;
			}else{
				channels.Add(new_channel);
				return true;
			}
		}
		
		public bool removeChannle(string channel){
			return channels.Remove(channel);
		}
		//project_path @ D://wwwww//demo/
		
		public void writeSettintToFile(string projectName){
			string project_path = System.IO.Path.Combine( Configration.projects_path,  Utils.generateSettingFileName(projectName));
			
			if(File.Exists(project_path)) File.Delete(project_path);

             using(Stream file = File.Open(project_path , FileMode.OpenOrCreate)){
				IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(file, this);
          
                Log.i("setting save succeed !!");
            }
            
		}
		
		public static ProjectConfigration readSettingFromFile(string projectName){
			string project_path = System.IO.Path.Combine( Configration.projects_path, Utils.generateSettingFileName(projectName));
			
			if(!File.Exists(project_path)){
				return null;
			}
			ProjectConfigration c;
            using (FileStream fs = new FileStream(project_path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                c = (ProjectConfigration)formatter.Deserialize(fs);
            }
			return c;
		}
	
	}
}
