﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using IOWrapper;
using UCR.Models.Devices;
using UCR.Models.Plugins;
using UCR.Models.Plugins.Remapper;

namespace UCR.Models
{
    public class UCRContext
    {
        // Persistence
        public List<Profile> Profiles { get; set; }
        public List<DeviceGroup<Keyboard>> KeyboardGroups { get; set; }
        public List<DeviceGroup<Mouse>> MiceGroups { get; set; }
        public List<DeviceGroup<Joystick>> JoystickGroups { get; set; }

        // Runtime
        public bool IsNotSaved { get; set; }
        public Profile ActiveProfile { get; set; }
        public IOController IOController { get; set; }

        public UCRContext()
        {
            IsNotSaved = false;
            IOController = new IOWrapper.IOController();
            Init();
        }

        public void Init()
        {
            KeyboardGroups = new List<DeviceGroup<Keyboard>>();
            MiceGroups = new List<DeviceGroup<Mouse>>();
            JoystickGroups = new List<DeviceGroup<Joystick>>();
            InitMock();
        }

        public void ActivateProfile(Profile profile)
        {
            bool success = true;
            success &= GetGlobalProfile().Activate(this);
            success &= profile.Activate(this);
            if (success)
            {
                ActiveProfile = profile;
                SubscribeDeviceLists();
            }
        }
        
        private void SubscribeDeviceLists()
        {

            foreach(var device in ActiveProfile.Joysticks.Devices)
            {
                device.Activate(this);
            }
            foreach (var device in ActiveProfile.Keyboards.Devices)
            {
                device.Activate(this);
            }
            foreach (var device in ActiveProfile.Mice.Devices)
            {
                device.Activate(this);
            }
        }

        private Profile GetGlobalProfile()
        {
            // TODO Find it properly
            return Profiles.Find(p => p.Title.Equals("Global"));
        }

        private void InitMock()
        {
            Profiles = new List<Profile>
            {
                new Profile()
                {
                    Title = "Global",
                    JoystickList = "FAKEGUID"
                },
                new Profile()
                {
                    Title = "N64"
                }
            };

            JoystickGroups = new List<DeviceGroup<Joystick>>()
            {
                new DeviceGroup<Joystick>()
                {
                    GUID = "FAKEGUID"
                }
            };
            JoystickGroups[0].Devices.Add(new Joystick(InputType.DirectInput)
            {
                Title = "Joystick mock name",
                Guid = "JOYSTICKGUID"
            });


            var list = IOController.GetInputList();
            string deviceHandle = null;

            for (var i = 0; i < list.Count && deviceHandle == null; i++)
            {
                foreach (var device in list[i].Devices)
                {
                    if (device.PluginName == "SharpDX_DirectInput")
                    {
                        JoystickGroups[0].Devices[0].Guid = device.DeviceHandle;
                        JoystickGroups[0].Devices[0].SubscriberPluginName = device.PluginName;
                        JoystickGroups[0].Devices[0].MaxButtons = (int)device.ButtonCount;
                        break;
                    }
                }
            }

            Profile global = GetGlobalProfile();

            for (int i = 0; i < JoystickGroups[0].Devices[0].MaxButtons; i++)
            {
                Plugin plugin;
                if (i % 2 == 0)
                {
                    plugin = new ButtonToButton(global)
                    {
                        Title = "ButtonToButton test " + i
                    };
                }
                else
                {
                    plugin = new ButtonToAxis(global)
                    {
                        Title = "ButtonToAxis test " + i
                    };
                }

                plugin.Inputs[0].DeviceType = DeviceType.Joystick;
                plugin.Inputs[0].KeyType = (int)KeyType.Button;
                plugin.Inputs[0].KeyValue = i;
                plugin.Outputs[0].KeyValue = i;

                global.AddPlugin(plugin);
            }
            


        }
    }
}
