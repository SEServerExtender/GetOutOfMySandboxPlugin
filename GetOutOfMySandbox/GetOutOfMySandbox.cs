namespace GetOutOfMySandbox
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Xml.Linq;
	using System.Xml.XPath;
	using NLog;
	using SEModAPIExtensions.API;
	using SEModAPIExtensions.API.Plugin;
	using SEModAPIInternal.API.Common;

	public class GetOutOfMySandbox : IPlugin
	{
		public static Logger Log;
		private static GetOutOfMySandbox _instance;
		private Thread _pluginThread;
		private bool _running;
		private static readonly object Locker = new object( );

		public void Init( )
		{
			string directoryName = Path.GetDirectoryName( Assembly.GetExecutingAssembly( ).Location );
			Log.Info( "Initializing GetOutOfMySandbox plugin at path {0}", directoryName );
			DoInit( directoryName );
		}

		public void InitWithPath( String modPath )
		{
			string directoryName = Path.GetDirectoryName( modPath ) + "\\";
			Log.Info( "Initializing GetOutOfMySandbox plugin at path {0}", directoryName );
			DoInit( directoryName );
		}

		private void DoInit( string path )
		{
			_instance = new GetOutOfMySandbox( );
			PluginPath = path;
			PluginSettings.Instance.Load( );
			_running = true;
			_pluginThread = new Thread( WaitLoop );
			_pluginThread.Start( );
		}

		private void WaitLoop( )
		{
			WorldManager.Instance.WorldSaved += Instance_WorldSaved;
			lock ( Locker )
			{
				while ( _running )
				{
					Monitor.Wait( Locker );
				}
			}
			WorldManager.Instance.WorldSaved -= Instance_WorldSaved;
			PluginSettings.Instance.Save( );
			Log.Info( "GetOutOfMySandbox {0} has shut down.", Version );
		}

		void Instance_WorldSaved( )
		{
			Log.Info( "Processing post-save cleanup events." );

			try
			{
				string sandboxPath = Path.Combine( Server.Instance.Config.LoadWorld, "Sandbox.sbc" );
				XDocument sandboxSbc = XDocument.Load( sandboxPath );
				XDocument sectorFile = XDocument.Load( Path.Combine( Server.Instance.Config.LoadWorld, "SANDBOX_0_0_0_.sbs" ) );
				IEnumerable<XElement> allIdentities = sandboxSbc.XPathSelectElements( "/MyObjectBuilder_Checkpoint/Identities/MyObjectBuilder_Identity/IdentityId" );
				IEnumerable<XElement> cubeBlockOwners = sectorFile.XPathSelectElements( "/MyObjectBuilder_Sector/SectorObjects/MyObjectBuilder_EntityBase/CubeBlocks/MyObjectBuilder_CubeBlock/Owner" );
				IEnumerable<XElement> factionMembers = sandboxSbc.XPathSelectElements( "/MyObjectBuilder_Checkpoint/Factions/Factions/MyObjectBuilder_Faction/Members/MyObjectBuilder_FactionMember/PlayerId" );

				List<string> idsToRemove = new List<string>( allIdentities.Select( o => o.Value ).Distinct( ).ToArray( ) );

				string[ ] cubeBlockOwnerIds = cubeBlockOwners.Select( o => o.Value ).Distinct( ).ToArray( );
				idsToRemove.RemoveAll( o => cubeBlockOwnerIds.Contains( o ) );

				if ( !PluginSettings.Instance.IgnoreFactionMembership )
				{
					string[ ] factionMemberIds = factionMembers.Select( f => f.Value ).Distinct( ).ToArray( );
					idsToRemove.RemoveAll( o => factionMemberIds.Contains( o ) );
				}

				foreach ( string i in idsToRemove )
				{
					Log.Info( "Removing identity {0}", i );
					//Remove toolbar settings, etc
					XElement settingsElement = sandboxSbc.XPathSelectElement( string.Format( "/MyObjectBuilder_Checkpoint/AllPlayersData/dictionary/item[Value/IdentityId='{0}']", i ) );
					if ( settingsElement != null )
					{
						settingsElement.Remove( );
					}

					//Remove from factions
					sandboxSbc.XPathSelectElements( string.Format( "/MyObjectBuilder_Checkpoint/Factions/Factions/MyObjectBuilder_Faction/Members/MyObjectBuilder_FactionMember[PlayerId='{0}']", i ) ).Remove( );
					//Remove chat history
					//XElement chatHistoryElement = sandboxSbc.XPathSelectElement( string.Format( "/MyObjectBuilder_Checkpoint/ChatHistory/MyObjectBuilder_ChatHistory/PlayerChatHistory/MyObjectBuilder_PlayerChatHistory[ID='{0}']", i ) );
					//chatHistoryElement.Remove( );
					//sandboxSbc.XPathSelectElement( string.Format( "/MyObjectBuilder_Checkpoint/ChatHistory/MyObjectBuilder_ChatHistory/[IdentityId='{0}']", i ) ).Remove( );
					//Remove GPS entries
					sandboxSbc.XPathSelectElements( string.Format( "/MyObjectBuilder_Checkpoint/Gps/dictionary/item[Key='{0}']", i ) ).Remove(  );
					//Remove the identity
					sandboxSbc.XPathSelectElement( string.Format( "/MyObjectBuilder_Checkpoint/Identities/MyObjectBuilder_Identity[IdentityId='{0}']", i ) ).Remove( );
				}

				sandboxSbc.Save( sandboxPath );
			}
			catch ( FileNotFoundException ex )
			{
				Log.Error( ex );
			}
			catch ( Exception ex )
			{
				Log.Error( ex );
			}

			Log.Info( "Finished processing post-save cleanup events." );
		}

		public void Update( )
		{

		}

		public void Shutdown( )
		{
			Log.Info( "Shutting down GetOutOfMySandbox - {0}", Version );
			lock ( Locker )
			{
				_running = false;
				Monitor.Pulse( Locker );
			}
		}

		/// <summary>Singleton instance of the core <see cref="GetOutOfMySandbox"/> plugin class.</summary>
		internal static GetOutOfMySandbox Instance
		{
			get { return _instance ?? ( _instance = new GetOutOfMySandbox( ) ); }
		}

		public Guid Id
		{
			get
			{
				GuidAttribute guidAttr = (GuidAttribute)typeof( GetOutOfMySandbox ).Assembly.GetCustomAttributes( typeof( GuidAttribute ), true )[ 0 ];
				return new Guid( guidAttr.Value );
			}
		}

		public string Name { get { return "Get Out Of My Sandbox!"; } }

		public Version Version { get { return typeof( GetOutOfMySandbox ).Assembly.GetName( ).Version; } }

		public static string PluginPath { get; set; }

		[Category( "Behavior" )]
		[Description( "Delete identities even if they belong to a faction." )]
		[Browsable( true )]
		[ReadOnly( false )]
		[DisplayName( "Ignore Faction Membership" )]
		public bool IgnoreFactionMembership
		{
			get
			{
				return PluginSettings.Instance.IgnoreFactionMembership;
			}

			set
			{
				PluginSettings.Instance.IgnoreFactionMembership = value;
			}
		}

	}
}
