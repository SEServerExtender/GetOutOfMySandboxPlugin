namespace GetOutOfMySandbox
{
	using System;
	using System.IO;
	using System.Security;
	using System.Xml.Serialization;

	[Serializable]
	[XmlType( Namespace = "SESE:Plugin:GetOutOfMySandbox", AnonymousType = false, IncludeInSchema = true )]
	[XmlRoot( "Settings", Namespace = "SESE:Plugin:GetOutOfMySandbox" )]
	public class PluginSettings
	{
		private static PluginSettings _instance;
		private bool _loading;
		private bool _ignoreFactionMembership;

		/// <summary>Singleton instance of the <see cref="PluginSettings"/> class.</summary>
		internal static PluginSettings Instance { get { return _instance ?? ( _instance = new PluginSettings( ) ); } }

		/// <summary>Loads the settings for the plugin</summary>
		public void Load( )
		{
			_loading = true;

			try
			{
				lock ( this )
				{
					string fileName = Path.Combine( GetOutOfMySandbox.PluginPath, "GetOutOfMySandbox-Settings.xml" );
					if ( File.Exists( fileName ) )
					{
						using ( StreamReader reader = new StreamReader( fileName ) )
						{
							XmlSerializer deserializer = new XmlSerializer( typeof( PluginSettings ) );
							PluginSettings settings = (PluginSettings)deserializer.Deserialize( reader );
							reader.Close( );

							_instance = settings;
						}
					}
				}
			}
			catch ( FileNotFoundException ex )
			{
				GetOutOfMySandbox.Log.Error( ex );
			}
			catch ( DirectoryNotFoundException ex )
			{
				GetOutOfMySandbox.Log.Error( ex );
			}
			catch ( IOException ex )
			{
				GetOutOfMySandbox.Log.Error( ex );
			}
			catch ( InvalidOperationException ex )
			{
				GetOutOfMySandbox.Log.Error( ex );
			}
			finally
			{
				_loading = false;
			}
		}

		/// <summary>Saves the settings for the plugin</summary>
		public void Save( )
		{
			if ( _loading )
				return;
			lock ( this )
			{
				string fileName = Path.Combine( GetOutOfMySandbox.PluginPath, "GetOutOfMySandbox-Settings.xml" );
				try
				{
					using ( StreamWriter writer = new StreamWriter( fileName ) )
					{
						XmlSerializer x = new XmlSerializer( typeof( PluginSettings ) );
						x.Serialize( writer, _instance );
						writer.Close( );
					}
					GetOutOfMySandbox.Log.Info( "Saved settings for plugin GetOutOfMySandbox." );
				}
				catch ( FileNotFoundException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
				catch ( DirectoryNotFoundException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
				catch ( IOException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
				catch ( InvalidOperationException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
				catch ( SecurityException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
				catch ( UnauthorizedAccessException ex )
				{
					GetOutOfMySandbox.Log.Error( ex );
				}
			}
		}

		[XmlElement( Namespace = "SESE:Plugin:GetOutOfMySandbox", Type = typeof( bool ) )]
		public bool IgnoreFactionMembership
		{
			get { return _ignoreFactionMembership; }
			set
			{
				_ignoreFactionMembership = value;
				OnSettingsChanged( );
			}
		}

		public event SettingsChangedEventHandler SettingsChanged;

		protected virtual void OnSettingsChanged( )
		{
			if ( SettingsChanged != null )
			{
				SettingsChanged( );
			}
		}
	}

	public delegate void SettingsChangedEventHandler( );
}
