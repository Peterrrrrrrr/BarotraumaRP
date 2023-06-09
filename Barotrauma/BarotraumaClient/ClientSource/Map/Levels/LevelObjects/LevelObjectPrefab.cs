﻿using Barotrauma.Lights;
using Barotrauma.Particles;
using Barotrauma.SpriteDeformations;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace Barotrauma
{
    partial class LevelObjectPrefab : PrefabWithUintIdentifier, ISerializableEntity
    {
        public class SoundConfig
        {
            public readonly ContentXElement SoundElement;
            public readonly Vector2 Position;
            public readonly int TriggerIndex;

            public SoundConfig(ContentXElement element, int triggerIndex)
            {
                SoundElement = element;
                Position = element.GetAttributeVector2("position", Vector2.Zero);
                TriggerIndex = triggerIndex;
            }
        }

        public List<int> ParticleEmitterTriggerIndex
        {
            get;
            private set;
        }
        public List<ParticleEmitterPrefab> ParticleEmitterPrefabs
        {
            get;
            private set;
        }
        public List<Vector2> EmitterPositions
        {
            get;
            private set;
        }

        public List<SoundConfig> Sounds
        {
            get;
            private set;
        } = new List<SoundConfig>();

        public List<int> LightSourceTriggerIndex
        {
            get;
            private set;
        } = new List<int>();
        public List<LightSourceParams> LightSourceParams
        {
            get;
            private set;
        } = new List<Lights.LightSourceParams>();

        /// <summary>
        /// Only used for editing sprite deformation parameters. The actual LevelObjects use separate SpriteDeformation instances.
        /// </summary>
        public List<SpriteDeformation> SpriteDeformations
        {
            get;
            private set;
        } = new List<SpriteDeformation>();

        partial void InitProjSpecific(ContentXElement element)
        {
            LoadElementsProjSpecific(element, -1);
        }

        private void LoadElementsProjSpecific(ContentXElement element, int parentTriggerIndex)
        {
            foreach (var subElement in element.Elements())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "leveltrigger":
                    case "trigger":
                        LoadElementsProjSpecific(subElement, LevelTriggerElements.IndexOf(subElement));
                        break;
                    case "lightsource":
                        LightSourceTriggerIndex.Add(parentTriggerIndex);
                        LightSourceParams.Add(new LightSourceParams(subElement));
                        break;
                    case "particleemitter":
                        if (ParticleEmitterPrefabs == null)
                        {
                            ParticleEmitterPrefabs = new List<ParticleEmitterPrefab>();
                            EmitterPositions = new List<Vector2>();
                            ParticleEmitterTriggerIndex = new List<int>();
                        }

                        ParticleEmitterPrefabs.Add(new ParticleEmitterPrefab(subElement));
                        ParticleEmitterTriggerIndex.Add(parentTriggerIndex);
                        EmitterPositions.Add(subElement.GetAttributeVector2("position", Vector2.Zero));
                        break;
                    case "sound":
                        Sounds.Add(new SoundConfig(subElement, parentTriggerIndex));
                        break;
                    case "deformablesprite":
                        foreach (XElement deformElement in subElement.Elements())
                        {
                            var deformation = SpriteDeformation.Load(deformElement, Name);
                            if (deformation != null)
                            {
                                SpriteDeformations.Add(deformation); 
                            }                           
                        }
                        break;
                }
            }
        }

        public void Save(XElement element)
        {
            this.Config = element;

            SerializableProperty.SerializeProperties(this, element);

            foreach (var subElement in element.Elements().ToList())
            {
                switch (subElement.Name.ToString().ToLowerInvariant())
                {
                    case "childobject":
                        subElement.Remove();
                        break;
                    case "deformablesprite":
                        subElement.RemoveNodes();
                        foreach (SpriteDeformation deformation in SpriteDeformations)
                        {
                            var deformationElement = new XElement("SpriteDeformation");
                            deformation.Save(deformationElement);
                            subElement.Add(deformationElement);
                        }
                        break;
                }
            }

            for (int i = 0; i < LightSourceParams.Count; i++)
            {
                int elementIndex = 0;
                bool wasSaved = false;
                foreach (var subElement in element.Elements().ToList())
                {
                    switch (subElement.Name.ToString().ToLowerInvariant())
                    {
                        case "lightsource":
                            if (elementIndex == i)
                            {
                                SerializableProperty.SerializeProperties(LightSourceParams[i], subElement);
                                wasSaved = true;
                                break;
                            }
                            elementIndex++;
                            break;
                    }
                }
                if (!wasSaved)
                {
                    var lightElement = new XElement("LightSource");
                    SerializableProperty.SerializeProperties(LightSourceParams[i], lightElement);
                    element.Add(lightElement);
                }
            }

            foreach (ChildObject childObj in ChildObjects)
            {
                element.Add(new XElement("ChildObject",
                    new XAttribute("names", string.Join(", ", childObj.AllowedNames)),
                    new XAttribute("mincount", childObj.MinCount),
                    new XAttribute("maxcount", childObj.MaxCount)));
            }

            foreach (KeyValuePair<Identifier, float> overrideCommonness in OverrideCommonness)
            {
                bool elementFound = false;
                foreach (var subElement in element.Elements())
                {
                    if (subElement.Name.ToString().Equals("overridecommonness", System.StringComparison.OrdinalIgnoreCase)
                        && subElement.GetAttributeIdentifier("leveltype", Identifier.Empty) == overrideCommonness.Key)
                    {
                        subElement.Attribute("commonness").Value = overrideCommonness.Value.ToString("G", CultureInfo.InvariantCulture);
                        elementFound = true;
                        break;
                    }
                }
                if (!elementFound)
                {
                    element.Add(new XElement("overridecommonness",
                        new XAttribute("leveltype", overrideCommonness.Key),
                        new XAttribute("commonness", overrideCommonness.Value.ToString("G", CultureInfo.InvariantCulture))));
                }
            }
        }
    }
}
