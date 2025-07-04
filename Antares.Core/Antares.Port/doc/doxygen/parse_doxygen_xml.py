import xml.etree.ElementTree as ET
import os

def parse_xml_to_markdown(xml_file_path, output_dir):
    tree = ET.parse(xml_file_path)
    root = tree.getroot()

    # Assuming the root is always a <doxygen> tag and contains one <compounddef>
    compounddef = root.find('compounddef')
    if compounddef is None:
        return # Not a compound definition file

    kind = compounddef.get('kind')
    name = compounddef.find('compoundname').text

    markdown_content = f'# {kind.capitalize()}: {name}\n\n'

    # Description
    brief_description_elem = compounddef.find('briefdescription')
    if brief_description_elem is not None:
        brief_para = brief_description_elem.find('para')
        if brief_para is not None and brief_para.text:
            markdown_content += f'## Brief Description\n{brief_para.text}\n\n'

    detailed_description_elem = compounddef.find('detaileddescription')
    if detailed_description_elem is not None:
        detailed_para = detailed_description_elem.find('para')
        if detailed_para is not None and detailed_para.text:
            markdown_content += f'## Detailed Description\n{detailed_para.text}\n\n'


    # Inheritance
    base_compounds = compounddef.findall('basecompoundref')
    if base_compounds:
        markdown_content += '## Inheritance\n'
        for base in base_compounds:
            markdown_content += f'- Inherits from: {base.text}\n'
        markdown_content += '\n'

    # Sections (enums, member variables, functions)
    for sectiondef in compounddef.findall('sectiondef'):
        section_kind = sectiondef.get('kind')
        if section_kind == 'public-type' or section_kind == 'protected-type' or section_kind == 'private-type':
            markdown_content += '## Types\n'
            for memberdef in sectiondef.findall('memberdef'):
                if memberdef.get('kind') == 'enum':
                    enum_name = memberdef.find('name').text
                    markdown_content += f'### Enum: {enum_name}\n'
                    for enumvalue in memberdef.findall('enumvalue'):
                        value_name = enumvalue.find('name').text
                        markdown_content += f'- {value_name}\n'
                    markdown_content += '\n'
                elif memberdef.get('kind') == 'typedef':
                    typedef_name = memberdef.find('name').text
                    typedef_type = memberdef.find('type').text
                    markdown_content += f'### Typedef: {typedef_name}\n'
                    markdown_content += f'- Type: {typedef_type}\n\n'

        elif section_kind == 'private-attrib' or section_kind == 'public-attrib' or section_kind == 'protected-attrib':
            markdown_content += '## Member Variables\n'
            for memberdef in sectiondef.findall('memberdef'):
                var_name = memberdef.find('name').text
                var_type = memberdef.find('type').text
                prot = memberdef.get('prot')
                markdown_content += f'- `{prot} {var_type} {var_name}`: {get_brief_description(memberdef)}\n'
            markdown_content += '\n'
        elif section_kind == 'public-func' or section_kind == 'protected-func' or section_kind == 'private-func' or section_kind == 'public-static-func':
            markdown_content += '## Functions\n'
            for memberdef in sectiondef.findall('memberdef'):
                func_name = memberdef.find('name').text
                func_type = memberdef.find('type').text
                prot = memberdef.get('prot')
                static_attr = 'static ' if memberdef.get('static') == 'yes' else ''
                args_string = memberdef.find('argsstring').text if memberdef.find('argsstring') is not None else '()'

                markdown_content += f'### {static_attr}{prot} {func_type} {func_name}{args_string}\n'
                markdown_content += f'{get_brief_description(memberdef)}\n'

                params = memberdef.findall('param')
                if params:
                    markdown_content += '#### Parameters:\n'
                    for param in params:
                        param_name = param.find('declname').text if param.find('declname') is not None else ''
                        param_type = param.find('type').text if param.find('type') is not None else ''
                        param_desc_elem = param.find('briefdescription')
                        param_desc = param_desc_elem.find('para').text if param_desc_elem is not None and param_desc_elem.find('para') is not None else ''
                        markdown_content += f'- `{param_type} {param_name}`: {param_desc}\n'
                markdown_content += '\n'

    output_filename = os.path.join(output_dir, f'{name.replace("::", "_")}.md')
    with open(output_filename, 'w') as f:
        f.write(markdown_content)

def get_brief_description(element):
    brief = element.find('briefdescription')
    if brief is not None and brief.find('para') is not None and brief.find('para').text:
        return brief.find('para').text
    return ''

# Main execution logic
xml_input_dir = '/home/send2/.projects/Quantlib/doc/doxygen/xml/'
markdown_output_dir = '/home/send2/.projects/Quantlib/doc/doxygen/md/'

for filename in os.listdir(xml_input_dir):
    if filename.endswith('.xml') and (filename.startswith('class') or filename.startswith('struct')):
        xml_file_path = os.path.join(xml_input_dir, filename)
        parse_xml_to_markdown(xml_file_path, markdown_output_dir)

print(f'Successfully converted XML files from {xml_input_dir} to Markdown in {markdown_output_dir}')
